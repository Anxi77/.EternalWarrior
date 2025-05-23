using System;
using Cinemachine;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class CameraSystem : MonoBehaviour, IInitializable
{
    public bool IsInitialized { get; private set; }

    [Header("Camera Settings")]
    [SerializeField]
    private GameObject virtualCameraPrefab;

    [SerializeField]
    private float townCameraSize = 8f;

    [SerializeField]
    private float gameCameraSize = 6f;
    private CinemachineVirtualCamera virtualCamera;

    public void Initialize()
    {
        try
        {
            virtualCameraPrefab = Resources.Load<GameObject>("Prefabs/Camera/VirtualCamera");
            if (virtualCameraPrefab == null)
            {
                Logger.LogWarning(typeof(CameraSystem), "Virtual Camera Prefab not found!");
                return;
            }
            IsInitialized = true;
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(CameraSystem), $"Error initializing CameraManager: {e.Message}");
            IsInitialized = false;
        }
    }

    public void SetupCamera(SceneType sceneType)
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Logger.LogError(
                typeof(CameraSystem),
                "Main Camera not found! Looking for camera in scene..."
            );
            mainCamera = FindObjectOfType<Camera>();
            if (mainCamera == null)
            {
                Logger.LogError(typeof(CameraSystem), "No camera found in scene at all!");
                return;
            }
        }

        var brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
        }

        brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;

        if (virtualCameraPrefab == null)
        {
            Logger.LogError(typeof(CameraSystem), "Virtual Camera Prefab is not assigned!");
            return;
        }

        try
        {
            if (virtualCamera != null)
            {
                Destroy(virtualCamera.gameObject);
            }

            GameObject camObj = Instantiate(virtualCameraPrefab, mainCamera.transform);
            camObj.transform.localPosition = Vector3.zero;
            virtualCamera = camObj.GetComponent<CinemachineVirtualCamera>();

            if (virtualCamera == null)
            {
                Logger.LogError(
                    typeof(CameraSystem),
                    "Failed to get CinemachineVirtualCamera component!"
                );
                return;
            }

            switch (sceneType)
            {
                case SceneType.Main_Town:
                    virtualCamera.m_Lens.OrthographicSize = townCameraSize;
                    break;
                case SceneType.Main_Stage:
                    virtualCamera.m_Lens.OrthographicSize = gameCameraSize;
                    break;
            }

            if (GameManager.Instance.PlayerSystem.Player != null)
            {
                virtualCamera.Follow = GameManager.Instance.PlayerSystem.Player.transform;
            }
            else
            {
                Logger.LogWarning(typeof(CameraSystem), "Player not found for camera to follow!");
            }
        }
        catch (Exception e)
        {
            Logger.LogError(typeof(CameraSystem), $"Error setting up camera: {e.Message}");
        }
    }

    public void ClearCamera()
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = null;
            Destroy(virtualCamera.gameObject);
            virtualCamera = null;
        }
    }
}
