#if UNITY_EDITOR
using Game.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Utils
{
	/// <summary>
	/// One-click setup for local multiplayer input in PlayScene: creates the player prefab
	/// (<see cref="PlayerInput"/> + <see cref="LocalPlayerInputController"/>) and a scene object with
	/// <see cref="PlayerInputManager"/> + <see cref="LocalMultiplayerInputManager"/>. Safe to run again.
	/// </summary>
	public static class LocalMultiplayerSetupUtil
	{
		private const string ACTIONS_PATH = "Assets/AppGameConfigs/InputSystem_Actions.inputactions";
		private const string PLAYER_PREFAB_PATH = "Assets/!/Content/Game/Input/LocalPlayerInput.prefab";
		private const string PLAY_SCENE_PATH = "Assets/!/Scenes/PlayScene.unity";
		private const string MANAGER_OBJECT_NAME = "LocalMultiplayerInput";
		private const string PLAYER_MAP = "Player";

		[MenuItem("Tools/Game/Setup Local Multiplayer Input")]
		private static void Setup()
		{
			var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ACTIONS_PATH);
			if (actions == null)
			{
				Debug.LogError($"Input actions asset not found at {ACTIONS_PATH}");
				return;
			}

			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				return;

			var playerPrefab = CreatePlayerPrefab(actions);
			SetupPlayScene(playerPrefab);
		}

		private static GameObject CreatePlayerPrefab(InputActionAsset actions)
		{
			var root = new GameObject("LocalPlayerInput");
			var playerInput = root.AddComponent<PlayerInput>();
			playerInput.actions = actions;
			playerInput.defaultActionMap = PLAYER_MAP;
			playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
			root.AddComponent<LocalPlayerInputController>();

			var prefab = PrefabUtility.SaveAsPrefabAsset(root, PLAYER_PREFAB_PATH);
			Object.DestroyImmediate(root);
			return prefab;
		}

		private static void SetupPlayScene(GameObject playerPrefab)
		{
			var scene = EditorSceneManager.OpenScene(PLAY_SCENE_PATH, OpenSceneMode.Single);

			var managerObject = GameObject.Find(MANAGER_OBJECT_NAME);
			if (managerObject == null)
				managerObject = new GameObject(MANAGER_OBJECT_NAME);

			var inputManager = managerObject.GetComponent<PlayerInputManager>();
			if (inputManager == null)
				inputManager = managerObject.AddComponent<PlayerInputManager>();

			if (managerObject.GetComponent<LocalMultiplayerInputManager>() == null)
				managerObject.AddComponent<LocalMultiplayerInputManager>();

			// maxPlayerCount has no public setter, so configure everything through the serialized fields.
			var serialized = new SerializedObject(inputManager);
			serialized.FindProperty("m_NotificationBehavior").enumValueIndex = (int)PlayerNotifications.InvokeCSharpEvents;
			serialized.FindProperty("m_JoinBehavior").enumValueIndex = (int)PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
			serialized.FindProperty("m_PlayerPrefab").objectReferenceValue = playerPrefab;
			serialized.FindProperty("m_AllowJoining").boolValue = true;
			serialized.FindProperty("m_SplitScreen").boolValue = false;
			serialized.ApplyModifiedPropertiesWithoutUndo();

			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			Selection.activeGameObject = managerObject;
			Debug.Log($"Local multiplayer input set up in {PLAY_SCENE_PATH}");
		}
	}
}
#endif
