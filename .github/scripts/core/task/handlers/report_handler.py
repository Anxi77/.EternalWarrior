"""
태스크 리포트 관리 핸들러
"""
import logging
import time
import os
from datetime import datetime
from typing import Dict, Optional, Tuple

logger = logging.getLogger(__name__)

class ReportHandler:
    def __init__(self, github_client, project_name: str):
        """
        태스크 리포트 핸들러 초기화
        
        Args:
            github_client: GitHub API 클라이언트
            project_name: 프로젝트 이름
        """
        self.client = github_client
        self.project_name = project_name
        self.cache_file = os.path.join(os.path.dirname(__file__), "../../../.cache/last_report_update.txt")
        os.makedirs(os.path.dirname(self.cache_file), exist_ok=True)

    def _should_update_report(self) -> bool:
        """업데이트 진행 여부를 결정합니다 (쿨다운 적용)"""
        cooldown_period = 300  # 5분
        current_time = time.time()
        
        try:
            if os.path.exists(self.cache_file):
                with open(self.cache_file, 'r') as f:
                    last_update = float(f.read().strip() or 0)
                if (current_time - last_update) < cooldown_period:
                    logger.info(f"마지막 업데이트 이후 {cooldown_period}초가 지나지 않았습니다. 건너뜁니다.")
                    return False
        except Exception as e:
            logger.warning(f"쿨다운 확인 중 오류: {str(e)}")
        
        with open(self.cache_file, 'w') as f:
            f.write(str(current_time))
        
        return True

    def _is_automated_update(self) -> bool:
        """자동화된 업데이트인지 확인합니다"""
        event_name = os.environ.get('GITHUB_EVENT_NAME')
        actor = os.environ.get('GITHUB_ACTOR')
        return actor == 'github-actions[bot]'

    def create_or_update_report(self, report_formatter) -> None:
        """프로젝트 보고서를 생성하거나 업데이트합니다."""
        logger.info("프로젝트 보고서 생성/업데이트 시작")
        
        if self._is_automated_update():
            logger.info("자동화된 업데이트 감지. 재귀 방지를 위해 건너뜁니다.")
            return
            
        if not self._should_update_report():
            return
        
        repo_id, labels = self._get_repository_id()
        if not repo_id:
            logger.error("저장소 ID를 가져오는데 실패했습니다.")
            return
        
        if 'report' not in labels:
            logger.info("'report' 라벨이 없어 새로 생성합니다...")
            label_id = self._create_report_label(repo_id)
            if label_id:
                labels['report'] = label_id
        
        report_title = f"📊 프로젝트 진행보고서 - {self.project_name}"
        report_body = report_formatter.format_report()
        
        existing_report = self._find_existing_report()
        
        if existing_report:
            self._update_report(existing_report, report_title, report_body)
        else:
            self._create_report(repo_id, labels, report_title, report_body)

    def _get_repository_id(self) -> Tuple[Optional[str], Dict[str, str]]:
        """저장소의 ID를 가져옵니다."""
        query = """
        query($org: String!, $name: String!) {
            organization(login: $org) {
                repository(name: $name) {
                    id
                    labels(first: 100) {
                        nodes {
                            id
                            name
                        }
                    }
                }
            }
        }
        """
        
        variables = {
            "org": self.client.org,
            "name": self.project_name
        }
        
        result = self.client._execute_graphql(query, variables)
        if result and 'organization' in result and 'repository' in result['organization']:
            repo = result['organization']['repository']
            labels = {
                label['name']: label['id']
                for label in repo['labels']['nodes']
            }
            return repo['id'], labels
        return None, {}

    def _create_report_label(self, repo_id: str) -> Optional[str]:
        """report 라벨을 생성합니다."""
        create_label_mutation = """
        mutation($repositoryId: ID!, $name: String!, $description: String!, $color: String!) {
            createLabel(input: {
                repositoryId: $repositoryId,
                name: $name,
                description: $description,
                color: $color
            }) {
                label {
                    id
                }
            }
        }
        """
        
        variables = {
            "repositoryId": repo_id,
            "name": "report",
            "description": "프로젝트 보고서 관련 이슈",
            "color": "0E8A16"  # 초록색
        }
        
        result = self.client._execute_graphql(create_label_mutation, variables)
        if result and 'createLabel' in result:
            label_id = result['createLabel']['label']['id']
            logger.info("'report' 라벨이 성공적으로 생성되었습니다.")
            return label_id
        else:
            logger.error("'report' 라벨 생성 실패")
            return None

    def _find_existing_report(self) -> Optional[Dict]:
        """기존 보고서를 찾습니다."""
        query = """
        query($org: String!, $name: String!) {
            organization(login: $org) {
                repository(name: $name) {
                    issues(first: 10, states: OPEN, labels: ["report"], orderBy: {field: CREATED_AT, direction: DESC}) {
                        nodes {
                            id
                            number
                            title
                            createdAt
                        }
                    }
                }
            }
        }
        """
        
        variables = {
            "org": self.client.org,
            "name": self.project_name
        }
        
        result = self.client._execute_graphql(query, variables)
        
        if result and 'organization' in result and 'repository' in result['organization']:
            issues = result['organization']['repository']['issues']['nodes']
            
            if issues:
                existing_report = issues[0] 
                logger.info(f"최근 보고서 #{existing_report['number']} 발견: {existing_report['title']}")
                return existing_report
        
        return None

    def _update_report(self, existing_report: Dict, title: str, body: str) -> None:
        """기존 보고서를 업데이트합니다."""
        update_query = """
        mutation($id: ID!, $title: String!, $body: String!) {
            updateIssue(input: {id: $id, title: $title, body: $body}) {
                issue {
                    number
                }
            }
        }
        """
        
        variables = {
            "id": existing_report['id'],
            "title": title,
            "body": body
        }
        
        result = self.client._execute_graphql(update_query, variables)
        if result:
            logger.info(f"보고서 #{existing_report['number']} 업데이트 완료")
        else:
            logger.error("보고서 업데이트 실패")

    def _create_report(self, repo_id: str, labels: Dict[str, str], title: str, body: str) -> None:
        """새 보고서를 생성합니다."""
        create_query = """
        mutation($repositoryId: ID!, $title: String!, $body: String!, $labelIds: [ID!]) {
            createIssue(input: {
                repositoryId: $repositoryId,
                title: $title,
                body: $body,
                labelIds: $labelIds
            }) {
                issue {
                    number
                }
            }
        }
        """
        
        label_ids = []
        if 'report' in labels:
            label_ids.append(labels['report'])
        else:
            logger.warning("'report' 라벨을 찾을 수 없습니다.")
        
        variables = {
            "repositoryId": repo_id,
            "title": title,
            "body": body,
            "labelIds": label_ids
        }
        
        result = self.client._execute_graphql(create_query, variables)
        if result and 'createIssue' in result:
            issue_number = result['createIssue']['issue']['number']
            logger.info(f"새 보고서 #{issue_number} 생성 완료")
        else:
            logger.error(f"보고서 생성 실패: {result}") 