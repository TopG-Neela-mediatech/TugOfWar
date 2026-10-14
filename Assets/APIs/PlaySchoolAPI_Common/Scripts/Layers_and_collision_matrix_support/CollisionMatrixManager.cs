using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class CollisionMatrixManager : SerializedMonoBehaviour
{
        private static CollisionMatrixManager _instance;

        public static CollisionMatrixManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CollisionMatrixManager>();

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<CollisionMatrixManager>();
                        singletonObject.name = typeof(CollisionMatrixManager).ToString() + " (Singleton)";
                        
        
                    }
                }

                return _instance;
            }
        }
    
  
    public CollisionMatrixSO CustomMatrix;
    public CollisionMatrixSO PlaySchoolMatrix;
    string tagManagerPath = "ProjectSettings/TagManager.asset";

    private void Awake()
    {
        
        SavePlayschoolData();
        LoadCustomData();
    }

    private void OnDestroy()
    {
        LoadPlayschoolData();
    }
    

    #region Collision Matrix

    #if PLAYSCHOOL_MAIN
    [Button]
    #endif
    public void SavePlayschoolData()
    {
        // SaveLayers(PlaySchoolMatrix);
        SaveLayerCollisions(PlaySchoolMatrix);
    }

    public void LoadPlayschoolData()
    {
      //  LoadLayers(PlaySchoolMatrix);
        LoadCollisionLayers(PlaySchoolMatrix);
    }

    #if !PLAYSCHOOL_MAIN
    [Button]
    #endif
    public void SaveCustomData()
    {
     //   SaveLayers(CustomMatrix);
        SaveLayerCollisions(CustomMatrix);
    }

    public void LoadCustomData()
    {
        LoadCollisionLayers(CustomMatrix);
       // LoadLayers(CustomMatrix);
    }


    void SaveLayerCollisions(CollisionMatrixSO matrix)
    {
        // Unity supports 32 layers (0 to 31)
        int totalLayers = 32;        //matrix = "";

        // Loop through all possible layer combinations
        for (int i = 0; i < totalLayers; i++)
        {
            if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) continue;
            for (int j = i; j < totalLayers; j++) // Check only once per pair
                {
                    if (string.IsNullOrEmpty(LayerMask.LayerToName(j))) continue;   
                    matrix.CollisionMatrixData[i, j] = !Physics.GetIgnoreLayerCollision(i, j);                                   
                }
        }
        Debug.Log("Saved Collision Matrix");
        
    }

    void LoadCollisionLayers(CollisionMatrixSO matrix)
    {
        int totalLayers = matrix.CollisionMatrixData.GetLength(0);

        for (int i = 0; i < totalLayers; i++)
        {
            // Skip unused layers
            //if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) continue;

            for (int j = i; j < totalLayers; j++)
            {
                // Skip unused layers
                //if (string.IsNullOrEmpty(LayerMask.LayerToName(j))) continue;
                Physics.IgnoreLayerCollision(i,j, !matrix.CollisionMatrixData[i, j]);
            }
        }
        Debug.Log("Loaded Collision Matrix");
    }

    

    #endregion

    #region Layers

    public void SaveLayers(CollisionMatrixSO matrix)
    {
        
        // check existance
        if (!File.Exists(tagManagerPath))
        {
            UnityEngine.Debug.LogError("TagManager.asset file not found.");
            return;
        }

        //Read lines
        string[] lines = File.ReadAllLines(tagManagerPath);

        //Save Layers
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith("layers:"))
            {
                // Find the next empty layer slot
                for (int j = i + 1; j < i+32; j++)
                {
                    matrix.CollisionLayerData[j-i-1] = lines[j];
                }
                return;
            }
        }

    Debug.Log("Layers Saved");
    }

    public void LoadLayers(CollisionMatrixSO matrix)
    {
        if (!File.Exists(tagManagerPath))
        {
            UnityEngine.Debug.LogError("TagManager.asset file not found.");
            return;
        }

        //Read lines
        string[] lines = File.ReadAllLines(tagManagerPath);

        //Save Layers
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().StartsWith("layers:"))
            {
                // Find the next empty layer slot
                for (int j = i + 1; j < i+32; j++)
                {
                        lines[j] = matrix.CollisionLayerData[j-i-1];
                        File.WriteAllLines(tagManagerPath, lines);
                    // if (lines[j].Trim() == "-")
                    // {
                        
                    // }
                    
                }
               #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
                #endif
                return;
            }
        }
        Debug.Log("Load Layers");
    }
    #endregion
   
}
