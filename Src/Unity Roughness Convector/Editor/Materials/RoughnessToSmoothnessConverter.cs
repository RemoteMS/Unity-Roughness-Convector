#if UNITY_EDITOR
namespace Editor.Materials
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;

    public class RoughnessToSmoothnessConverter : EditorWindow
    {
        private Texture2D _roughnessTexture;

        private bool _pinWindow = true;
        private bool _useSourceFolder = true;

        private string _customFolderPath;

        private string _errorMessage = "";
        private MessageType _errorType = MessageType.None;

        [MenuItem("Tools/Roughness To Smoothness Converter")]
        public static void ShowWindow()
        {
            var window = GetWindow<RoughnessToSmoothnessConverter>("Roughness Converter");
            window.titleContent = new GUIContent("Roughness Converter");
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical();

            GUILayout.Label("Roughness → Smoothness Converter", EditorStyles.boldLabel);

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            var newPin = EditorGUILayout.Toggle(_pinWindow, GUILayout.Width(20));
            GUILayout.Label("Pin Window");
            GUILayout.EndHorizontal();

            if (newPin != _pinWindow)
            {
                _pinWindow = newPin;
                UpdateWindowMode();
            }

            GUILayout.Space(10);

            GUILayout.Label("Roughness Texture");

            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(100), GUILayout.Height(100));
            var newTexture = (Texture2D)EditorGUI.ObjectField(rect, _roughnessTexture, typeof(Texture2D), false);

            if (newTexture != _roughnessTexture)
            {
                _roughnessTexture = newTexture;
                ClearError();
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            var newUseSourceFolder = EditorGUILayout.Toggle(_useSourceFolder, GUILayout.Width(20));
            GUILayout.Label("Use Source Folder");
            GUILayout.EndHorizontal();

            if (newUseSourceFolder != _useSourceFolder)
            {
                _useSourceFolder = newUseSourceFolder;
                ClearError();
            }

            if (!_useSourceFolder)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(
                    "Save Folder",
                    string.IsNullOrEmpty(_customFolderPath)
                        ? "Not selected"
                        : _customFolderPath
                );

                if (GUILayout.Button("Select", GUILayout.Width(80)))
                {
                    EditorApplication.delayCall += DelayedSelectFolder;
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            if (!string.IsNullOrEmpty(_errorMessage))
            {
                EditorGUILayout.HelpBox(_errorMessage, _errorType);
                GUILayout.Space(5);
            }

            if (GUILayout.Button("Convert"))
            {
                ConvertTexture();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
        }

        private void ClearError()
        {
            _errorMessage = "";
            _errorType = MessageType.None;
        }

        private void SetError(string message, MessageType type = MessageType.Error)
        {
            _errorMessage = message;
            _errorType = type;
        }

        private void DelayedSelectFolder()
        {
            EditorApplication.delayCall -= DelayedSelectFolder;
            SelectFolder();
        }

        private void SelectFolder()
        {
            var absolutePath = EditorUtility.OpenFolderPanel(
                "Select Save Folder",
                Application.dataPath,
                "");

            if (string.IsNullOrEmpty(absolutePath))
                return;

            if (!absolutePath.StartsWith(Application.dataPath))
            {
                SetError("Folder must be inside the project Assets folder.");
                return;
            }

            _customFolderPath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            ClearError();

            Repaint();
        }

        private void UpdateWindowMode()
        {
            var rect = position;
            var title = "Roughness Converter";

            Close();

            RoughnessToSmoothnessConverter newWindow;

            if (_pinWindow)
            {
                newWindow = CreateInstance<RoughnessToSmoothnessConverter>();
                newWindow.ShowUtility();
            }
            else
            {
                newWindow = GetWindow<RoughnessToSmoothnessConverter>();
                newWindow.Show();
            }

            newWindow.position = rect;
            newWindow.titleContent = new GUIContent(title);

            newWindow._pinWindow = _pinWindow;
            newWindow._useSourceFolder = _useSourceFolder;
            newWindow._roughnessTexture = _roughnessTexture;
            newWindow._customFolderPath = _customFolderPath;
        }

        private void ConvertTexture()
        {
            ClearError();

            if (!_roughnessTexture)
            {
                SetError("No texture assigned");
                Debug.LogError("No texture assigned");
                return;
            }

            var sourcePath = AssetDatabase.GetAssetPath(_roughnessTexture);

            if (string.IsNullOrEmpty(sourcePath))
            {
                SetError("Texture is not in the Assets folder");
                Debug.LogError("Texture is not in the Assets folder");
                return;
            }

            string directory;

            if (_useSourceFolder)
            {
                directory = Path.GetDirectoryName(sourcePath);
            }
            else
            {
                if (string.IsNullOrEmpty(_customFolderPath))
                {
                    SetError("No save folder selected");
                    Debug.LogError("No save folder selected");
                    return;
                }

                directory = _customFolderPath;
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(sourcePath);

            var readable = importer.isReadable;

            if (!readable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();

                _roughnessTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            }

            var source = _roughnessTexture;

            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);

            var pixels = source.GetPixels();

            for (var i = 0; i < pixels.Length; i++)
            {
                var inverted = 1f - pixels[i].grayscale;
                pixels[i] = new Color(inverted, inverted, inverted, 1);
            }

            result.SetPixels(pixels);
            result.Apply();

            var png = result.EncodeToPNG();

            var filename = Path.GetFileNameWithoutExtension(sourcePath);

            var newPath = Path.Combine(directory, filename + "_Smoothness.png");

            try
            {
                File.WriteAllBytes(newPath, png);
                AssetDatabase.Refresh();

                SetError("Smoothness texture created at: "  + newPath, MessageType.Info);
                Debug.Log("Smoothness texture created at: " + newPath);
            }
            catch (System.Exception e)
            {
                SetError("Error saving texture: "       + e.Message);
                Debug.LogError("Error saving texture: " + e.Message);
            }

            if (!readable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif