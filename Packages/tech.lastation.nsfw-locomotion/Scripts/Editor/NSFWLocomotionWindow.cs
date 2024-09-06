using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace Lastation.NSFWLocomotion
{
    public class NSFWLocomotionWindow : EditorWindow
    {
        private VRCAvatarDescriptor[] avatarDescriptorsFromScene;
        private VRCAvatarDescriptor avatarDescriptor;
        private Vector2 scrollPosDescriptors;
        private bool refreshedAvatars;
        private bool showWDButtons;

        private enum Version
        {
            None,
            Full,
            Poses
        }

        private const string FullVersionPrefabPath    = "Packages/tech.lastation.nsfw-locomotion/Resources/Prefabs/Full/NSFW Locomotion Full (VRCFury).prefab";
        private const string FullVersionWDPrefabPath  = "Packages/tech.lastation.nsfw-locomotion/Resources/Prefabs/Full/NSFW Locomotion Full WD (VRCFury).prefab";
        private const string PosesVersionPrefabPath   = "Packages/tech.lastation.nsfw-locomotion/Resources/Prefabs/Poses/NSFW Locomotion Poses (VRCFury).prefab";
        private const string PosesVersionWDPrefabPath = "Packages/tech.lastation.nsfw-locomotion/Resources/Prefabs/Poses/NSFW Locomotion Poses WD (VRCFury).prefab";

        private const string LocoIcon   = "Packages/tech.lastation.nsfw-locomotion/Scripts/Editor/Media/NSFWLocoLogo.png";
        private const string LocoBanner = "Packages/tech.lastation.nsfw-locomotion/Scripts/Editor/Media/NSFWLocoBanner.png";

        [MenuItem("LastationVRChat/NSFW Locomotion", false, 1)]
        private static void ShowWindow()
        {
            var window = GetWindow<NSFWLocomotionWindow>(false, "NSFW Locomotion");
            window.titleContent = new GUIContent
            {
                image = EditorGUIUtility.Load(LocoIcon) as Texture2D,
                text = "NSFW Locomotion",
                tooltip = "A custom NSFW version of GoGo Loco ♥"
            };
            window.minSize = new Vector2(350, 400);
            Debug.Log("NSFW Locomotion window shown.");
        }

        private void OnEnable()
        {
            Debug.Log("NSFWLocomotionWindow OnEnable called.");

            if (EditorApplication.isPlaying || avatarDescriptor != null)
            {
                Debug.Log("Editor is playing or avatarDescriptor is already set. Exiting OnEnable.");
                return;
            }

            RefreshDescriptors();
            if (avatarDescriptorsFromScene.Length == 1)
            {
                avatarDescriptor = avatarDescriptorsFromScene[0];
                Debug.Log("Single avatarDescriptor found and set.");
            }
            else
            {
                Debug.Log("No single avatarDescriptor found, or more than one avatarDescriptor in scene.");
            }
        }

        private void OnGUI()
        {
            //Debug.Log("NSFWLocomotionWindow OnGUI called.");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawHeader();

            if (EditorApplication.isPlaying)
            {
                DrawPlaymodeWarning();
            }
            else
            {
                DrawAvatarSelection();
                DrawVersionSelection();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawHeader()
        {
            //Debug.Log("Drawing header.");
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 40
            };

            var subTitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 35
            };

            var bannerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(LocoBanner);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(bannerTexture);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("NSFW Locomotion", titleStyle);
            EditorGUILayout.LabelField("by LastationVRChat", subTitleStyle);
            EditorGUILayout.Space(25);
        }

        private void DrawPlaymodeWarning()
        {
            //Debug.Log("Drawing Playmode warning.");
            var warningStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                wordWrap = true,
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1.0f, 0.5f, 0.5f) }
            };

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Warning: This tool is not designed to function in Play Mode. Please exit Play Mode to use this tool, then re-enter Play Mode to test the newly created prefab.", warningStyle);
            EditorGUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawAvatarSelection()
        {
            //Debug.Log("Drawing avatar selection.");
            using (new EditorGUILayout.HorizontalScope())
            {
                avatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", avatarDescriptor, typeof(VRCAvatarDescriptor), true);
                var buttonLabel = avatarDescriptorsFromScene.Length < 2 ? "Select From Scene" : "Refresh";
                var buttonWidth = avatarDescriptorsFromScene.Length < 2 ? 130f : 70f;
                if (GUILayout.Button(buttonLabel, GUILayout.Width(buttonWidth)))
                {
                    Debug.Log($"Button clicked: {buttonLabel}");
                    RefreshDescriptors();
                    if (avatarDescriptorsFromScene.Length == 1)
                    {
                        avatarDescriptor = avatarDescriptorsFromScene[0];
                        Debug.Log("Single avatarDescriptor found and set.");
                    }
                }
            }

            if (avatarDescriptorsFromScene.Length > 1)
            {
                scrollPosDescriptors = EditorGUILayout.BeginScrollView(scrollPosDescriptors, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
                EditorGUILayout.BeginVertical();
                foreach (var item in avatarDescriptorsFromScene)
                {
                    if (item == null)
                    {
                        Debug.LogWarning("Found a null avatarDescriptor, refreshing.");
                        RefreshDescriptors();
                    }
                    else if (GUILayout.Button(item.name))
                    {
                        avatarDescriptor = item;
                        Debug.Log($"Selected avatarDescriptor: {item.name}");
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.Space();
            }

            if (avatarDescriptor == null && !refreshedAvatars)
            {
                Debug.Log("avatarDescriptor is null and avatars have not been refreshed.");
                RefreshDescriptors();
                if (avatarDescriptorsFromScene.Length == 1)
                {
                    avatarDescriptor = avatarDescriptorsFromScene[0];
                    Debug.Log("Single avatarDescriptor found and set after refresh.");
                }
                refreshedAvatars = true;
            }
        }

        private void DrawVersionSelection()
        {
            //Debug.Log("Drawing version selection.");
            EditorGUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Choose a Version:");

            if (!RequirementsMet())
            {
                GUI.enabled = false;
                //Debug.Log("Requirements not met, disabling buttons.");
            }

            if (GUILayout.Button("Full Version"))
            {
                Debug.Log("Full Version button clicked.");
                InstallVersion(Version.Full);
            }

            if (GUILayout.Button("Poses Only Version"))
            {
                Debug.Log("Poses Only Version button clicked.");
                InstallVersion(Version.Poses);
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void RefreshDescriptors()
        {
            Debug.Log("Refreshing avatar descriptors.");
            avatarDescriptorsFromScene = SceneAsset.FindObjectsOfType<VRCAvatarDescriptor>();
            Array.Reverse(avatarDescriptorsFromScene);
            Debug.Log($"Found {avatarDescriptorsFromScene.Length} avatar descriptors.");
        }

        private bool RequirementsMet()
        {
            bool met = avatarDescriptor != null;
            //Debug.Log($"Requirements met: {met}");
            return met;
        }

        private void InstallVersion(Version version)
        {
            Debug.Log($"Installing version: {version}");
            int writeDefaultsOnCount = 0;
            int writeDefaultsOffCount = 0;

            foreach (var animLayer in avatarDescriptor.baseAnimationLayers)
            {
                if (animLayer.animatorController is AnimatorController controller)
                {
                    foreach (var layer in controller.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            if (state.state.writeDefaultValues)
                            {
                                writeDefaultsOnCount++;
                            }
                            else
                            {
                                writeDefaultsOffCount++;
                            }
                        }
                    }
                }
            }

            Debug.Log($"Write Defaults On Count: {writeDefaultsOnCount}, Off Count: {writeDefaultsOffCount}");

            var prefabPath = (writeDefaultsOffCount > writeDefaultsOnCount)
                ? (version == Version.Poses ? PosesVersionPrefabPath : FullVersionPrefabPath)
                : (version == Version.Poses ? PosesVersionWDPrefabPath : FullVersionWDPrefabPath);

            Debug.Log($"Prefab path determined: {prefabPath}");
            InstallPrefab(prefabPath);
        }

        private void InstallPrefab(string prefabPath)
        {
            Debug.Log($"Installing prefab from path: {prefabPath}");
            if (AssetExists(prefabPath))
            {
                var prefabToInstall = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefabToInstall != null)
                {
                    var prefabObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToInstall);
                    prefabObject.transform.SetParent(avatarDescriptor.transform, false);
                    prefabObject.transform.localPosition = Vector3.zero;
                    Debug.Log("Prefab installed successfully.");
                }
                else
                {
                    Debug.LogWarning("Prefab could not be loaded at the given path.");
                }
            }
            else
            {
                Debug.LogWarning("Asset does not exist at the given path.");
            }
        }

        private bool AssetExists(string path)
        {
            var assetGUID = AssetDatabase.AssetPathToGUID(path);
            bool exists = !string.IsNullOrEmpty(assetGUID);
            Debug.Log($"Does the Asset exists at path {path}?? {exists}");
            return exists;
        }
    }
}
