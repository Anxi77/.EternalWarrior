"""
프로젝트 상태 업데이트 스크립트
"""
import os
from core.github.handlers.project_handler import GitHubProjectHandler
from core.github.client import GitHubClient
from core.task.handlers.task_handler import TaskHandler

def main():
    """메인 함수"""
    try:
        github_token = os.environ.get('PAT') or os.environ.get('GITHUB_TOKEN')
        if not github_token:
            raise ValueError("GitHub 토큰이 설정되지 않았습니다.")
        
        github_client = GitHubClient(github_token)
        
        github_manager = GitHubProjectHandler(github_client)
        
        project_items = github_manager.get_project_items()
        task_issues = github_manager.get_task_issues()
        task_manager = TaskHandler(project_items, task_issues)
        github_manager.update_project_status(task_manager)
        
    except Exception as e:
        print(f"오류 발생: {str(e)}")
        raise

if __name__ == '__main__':
    main() 