using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hitsub.ExpressionFaceIconGenerator.Scripts.Editor
{
    public class ExpressionFaceIconGeneratorEditor : EditorWindow
    {
        private const int THUMBNAIL_SIZE = 200;
        public Animator AvatarAnimator;
        public List<AnimationClip> TargetAnimations = new List<AnimationClip>();

        private ReorderableList m_animationsReorderableList;
        private Texture2D m_backgroundActiveTexture;
        private Texture2D m_backgroundTexture;
        private Texture2D m_previewTexture;
        private AnimationViewerGenerator m_generator;
        private string m_exportPath;
        private Vector2 m_scrollPos;

        public ExpressionFaceIconGeneratorEditor()
        {
            titleContent = new GUIContent("FaceIconGenerator");
        }

        private void CreateList()
        {
            m_animationsReorderableList = new ReorderableList(TargetAnimations, typeof(AnimationClip), false, true, true, true)
            {
                headerHeight = EditorStyles.miniButton.fixedHeight + 50,
                elementHeight = EditorGUIUtility.singleLineHeight + 6
            };
            m_animationsReorderableList.drawHeaderCallback = rect =>
            {
                var labelRect = new Rect(rect) { height = EditorGUIUtility.singleLineHeight };
                EditorGUI.LabelField (labelRect, "Target Animations");
                
                //ボタン群
                var buttonClearRect = new Rect(rect.width - 70, rect.y, 70, EditorStyles.miniButton.fixedHeight);
                var buttonLogRect = new Rect(rect.width - 140, rect.y, 70, EditorStyles.miniButton.fixedHeight);
                if (GUI.Button(buttonClearRect, "Clear"))
                {
                    m_animationsReorderableList.list.Clear();
                }
                if (GUI.Button(buttonLogRect, "Export"))
                {
                    Export();
                }

                //D&D
                var dropRect = new Rect
                {
                    x = 5,
                    y = rect.y + EditorStyles.miniButton.fixedHeight + 5,
                    width = rect.width,
                    height = 40,
                };
                EditorGUI.BeginDisabledGroup(true);
                GUI.Button(dropRect, "Drop Animations HERE");
                EditorGUI.EndDisabledGroup();
                var id = GUIUtility.GetControlID(FocusType.Passive, rect);
                if (rect.Contains(Event.current.mousePosition))
                {
                    switch (Event.current.type)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                            DragAndDrop.AcceptDrag();
                            DragAndDrop.activeControlID = id;
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            break;
                        case EventType.DragExited:
                            foreach (var objectReference in DragAndDrop.objectReferences)
                            {
                                var clip = objectReference as AnimationClip;
                                if (clip != null && !TargetAnimations.Contains(clip))
                                {
                                    TargetAnimations.Add(clip);
                                    HandleUtility.Repaint();
                                }
                                RefreshPreview();
                            }
                            break;
                    }
                }

            };
            m_animationsReorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var obj = m_animationsReorderableList.list[index] as Object;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= 10;
                rect.x += 5;
                rect.y += 3;
                m_animationsReorderableList.list[index] = EditorGUI.ObjectField(rect, obj, typeof(AnimationClip), false);
            };
            m_animationsReorderableList.onAddCallback = list =>
            {
                list.list.Add(null);
            };
            m_animationsReorderableList.onRemoveCallback = list =>
            {
                if (list.index >= 0 && list.index < list.count)
                {
                    list.list.RemoveAt(list.index);
                }
            };
        }
        private void OnGUI()
        {
            minSize = new Vector2(300, 400);
            if (m_animationsReorderableList == null)
            {
                CreateList();
            }
            
            if (m_backgroundTexture == null)
            {
                var color = EditorGUIUtility.isProSkin
                    ? (Color) new Color32 (56, 56, 56, 255)
                    : (Color) new Color32 (194, 194, 194, 255);
                m_backgroundTexture = new Texture2D(1, 1);
                m_backgroundTexture.SetPixel(0, 0, color);
                m_backgroundTexture.Apply();
            }
            
            if (m_backgroundActiveTexture == null)
            {
                m_backgroundActiveTexture = new Texture2D(1, 1);
                m_backgroundActiveTexture.SetPixel(0, 0, Color.gray);
                m_backgroundActiveTexture.Apply();
            }
            
            m_scrollPos = GUILayout.BeginScrollView(m_scrollPos, GUILayout.ExpandWidth(true), GUILayout.Height(position.height - (200 + EditorStyles.toolbar.fixedHeight)));
            
            AvatarAnimator = EditorGUILayout.ObjectField("Avatar Animator", AvatarAnimator, typeof(Animator), true) as Animator;
            if (AvatarAnimator == null)
            {
                const string MSG = "アバターのAnimatorをセットしてください。\n" +
                                   "Hierarchyウィンドウにある、アバターのルートをドラッグ&ドロップすればOKです。";
                EditorGUILayout.HelpBox(MSG, MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            EditorGUILayout.Separator();
            
            DrawExportPath();

            if (string.IsNullOrEmpty(m_exportPath))
            {
                EditorGUILayout.HelpBox("アイコン画像を出力したいフォルダを上の四角形のエリアにドラッグ&ドロップしてください。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            if (!ExpressionFaceIconUtility.IsExistDirectory(m_exportPath))
            {
                EditorGUILayout.HelpBox("フォルダではなくファイルが設定されています。フォルダをドラッグ&ドロップしてください。", MessageType.Error);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.Separator();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                m_animationsReorderableList.DoLayoutList();
                if (check.changed)
                {
                    RefreshPreview();
                }
            }

            if (TargetAnimations.Count == 0)
            {
                EditorGUILayout.HelpBox("表情アニメーションを「Drop Animations Here」のエリアにドラッグ&ドロップしてください。\nまとめて追加できます。", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            EditorGUILayout.EndScrollView();
            
            DrawFooter();
        }

        private void DrawExportPath()
        {
            var pathRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(pathRect, "ExportPath : ");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(pathRect, " ", (string.IsNullOrEmpty(m_exportPath) ? "Drop Folder Here" : m_exportPath));
            EditorGUI.EndDisabledGroup();
            
            var id = GUIUtility.GetControlID(FocusType.Passive, pathRect);
            if (pathRect.Contains(Event.current.mousePosition))
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is DefaultAsset)
                        {
                            DragAndDrop.AcceptDrag();
                            DragAndDrop.activeControlID = id;
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        }
                        break;
                    case EventType.DragExited:
                        if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] is DefaultAsset directory)
                        {
                            m_exportPath = AssetDatabase.GetAssetPath(directory);
                        }
                        break;
                }
            }
        }
        
        private void DrawFooter()
        {
            var height = THUMBNAIL_SIZE + EditorGUIUtility.singleLineHeight;
            var footerRect = new Rect(0, position.height - height, position.width, height);

            GUI.BeginGroup(footerRect, EditorStyles.toolbar);
            GUI.Label(new Rect(0, 0, position.width, EditorGUIUtility.singleLineHeight), "Preview");
            GUI.DrawTexture(new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height), m_backgroundTexture, ScaleMode.StretchToFill);

            if (GUI.Button(new Rect(position.width - 100, 0, 100, EditorStyles.toolbarButton.fixedHeight), "Refresh Preview", EditorStyles.toolbarButton))
            {
                RefreshPreview();
            }

            var previewTextureRect = new Rect(position.width / 2 - THUMBNAIL_SIZE / 2f, EditorStyles.toolbar.fixedHeight, THUMBNAIL_SIZE, THUMBNAIL_SIZE);
            EditorGUI.DrawPreviewTexture(previewTextureRect, m_backgroundActiveTexture);
            if (m_previewTexture != null)
            {
                EditorGUI.DrawPreviewTexture(previewTextureRect, m_previewTexture);
            }
            GUI.EndGroup();
        }
        
        private IList<Texture2D> Render(IEnumerable<AnimationClip> clips)
        {
            var list = new List<Texture2D>();
            m_generator = new AnimationViewerGenerator();
            GameObject avatarCopy = null;
            try
            {
                //Generator生成処理
                avatarCopy = Instantiate(AvatarAnimator.gameObject);
                avatarCopy.SetActive(true);
                AvatarAnimator.gameObject.SetActive(false);
                m_generator.Begin(avatarCopy);

                //カメラ配置
                var animator = avatarCopy.GetComponent<Animator>();
                m_generator.ParentCameraTo(animator.isHuman
                    ? animator.GetBoneTransform(HumanBodyBones.Head)
                    : animator.transform);

                foreach (var clip in clips)
                {
                    var texture = new Texture2D(THUMBNAIL_SIZE, THUMBNAIL_SIZE, TextureFormat.RGB24, true);
                    m_generator.Render(clip, texture, 0f);
                    list.Add(texture);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                m_generator.Terminate();
                AvatarAnimator.gameObject.SetActive(true);
                if (avatarCopy != null)
                {
                    DestroyImmediate(avatarCopy);
                }
            }
            return list;
        }

        private void RefreshPreview()
        {
            var first = TargetAnimations.FirstOrDefault(clip => clip != null);
            m_previewTexture = first == null ? null : Render(new[] { first })[0];
        }

        private void Export()
        {
            var textures = Render(TargetAnimations);
            var directory = ExpressionFaceIconUtility.GetExportFullDirectoryPath(m_exportPath);
            
            for (var index = 0; index < textures.Count; index++)
            {
                var texture = textures[index];
                var png = texture.EncodeToPNG();
                var path = $"{directory}/{TargetAnimations[index].name}.png";
                
                System.IO.File.WriteAllBytes(path, png);
            }
        }

        [MenuItem("Window/Hitsub/FaceIconGenerator")]
        public static void ShowWindow()
        {
            GetWindow<ExpressionFaceIconGeneratorEditor>(false, null, false);
        }
    }
}
