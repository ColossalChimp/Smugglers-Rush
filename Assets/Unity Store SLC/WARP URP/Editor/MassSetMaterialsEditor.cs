using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace WARP
{
    public class MassSetMaterialsEditor : EditorWindow
    {
        private Material materialToAssign;
        private List<GameObject> cachedGameObjects = new List<GameObject>();
        private Dictionary<Transform, bool> foldoutStates = new Dictionary<Transform, bool>();
        private Vector2 scrollPosition;
        private float debounceTime = 0.1f;
        private double lastSelectionChangeTime = 0;

        [MenuItem("Tools/Mass Set Materials")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(MassSetMaterialsEditor), false, "Mass Set Materials");
        }

        private void OnEnable()
        {
            CacheAllGameObjectsInScene();
            EditorApplication.hierarchyChanged += OnHierarchyChange;
            Selection.selectionChanged += OnSelectionChange;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChange;
            Selection.selectionChanged -= OnSelectionChange;
        }

        private void OnHierarchyChange()
        {
            CacheAllGameObjectsInScene();
            Repaint();
        }

        private void OnSelectionChange()
        {
            if (EditorApplication.timeSinceStartup - lastSelectionChangeTime > debounceTime)
            {
                lastSelectionChangeTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void CacheAllGameObjectsInScene()
        {
            cachedGameObjects.Clear();
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            cachedGameObjects.AddRange(allObjects);
        }

        void OnGUI()
        {
            materialToAssign = (Material)EditorGUILayout.ObjectField("Material to Assign", materialToAssign, typeof(Material), false);

            GUILayout.Space(10);

            if (GUILayout.Button("Assign Material to Selected"))
            {
                AssignMaterialToSelected();
            }

            GUILayout.Space(20);
            GUILayout.Label("Selected GameObjects:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (Selection.gameObjects.Length > 0)
            {
                foreach (GameObject selectedObj in Selection.gameObjects)
                {
                    DisplayGameObjects(selectedObj.transform, 0);
                }
            }
            else
            {
                GUILayout.Label("No GameObjects selected.");
            }

            EditorGUILayout.EndScrollView();
        }

        void DisplayGameObjects(Transform obj, int indentLevel, int maxDepth = 5)
        {
            if (indentLevel > maxDepth) return;

            if (!foldoutStates.ContainsKey(obj))
                foldoutStates[obj] = false;

            GUILayout.BeginHorizontal();
            GUILayout.Space(indentLevel * 15);

            if (obj.childCount > 0)
            {
                foldoutStates[obj] = EditorGUILayout.Foldout(foldoutStates[obj], obj.name);
            }
            else
            {
                GUILayout.Label(obj.name);
            }

            GUILayout.EndHorizontal();

            if (obj.childCount > 0 && foldoutStates[obj])
            {
                foreach (Transform child in obj)
                {
                    DisplayGameObjects(child, indentLevel + 1, maxDepth);
                }
            }
        }

        void AssignMaterialToSelected()
        {
            if (materialToAssign == null)
            {
                Debug.LogWarning("Please assign a material before applying.");
                return;
            }

            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("No GameObjects selected. Please select at least one GameObject in the scene.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(Selection.gameObjects, "Mass Set Materials");

            foreach (GameObject obj in Selection.gameObjects)
            {
                AssignMaterialToChildren(obj.transform, materialToAssign);
            }
        }

        static void AssignMaterialToChildren(Transform parent, Material material)
        {
            MeshRenderer renderer = parent.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterials = new Material[] { material };
            }

            foreach (Transform child in parent)
            {
                AssignMaterialToChildren(child, material);
            }
        }
    }
}