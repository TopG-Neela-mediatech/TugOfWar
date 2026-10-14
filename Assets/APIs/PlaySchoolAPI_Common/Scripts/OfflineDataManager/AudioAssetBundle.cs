using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AudioAssetBundle
{
    private static Dictionary<string, Object> loadedAssets = new Dictionary<string, Object>();

    public static async Task LoadSceneAndAssetsAsync(string filePath, int sceneIndex, string audioPath, string audioAssetName)
    {
        // REMOVE: `UnloadAllAssetBundles` so loaded assets are not cleared
        // AssetBundle.UnloadAllAssetBundles(true);

        //string filePath = Path.Combine(Application.streamingAssetsPath, bundleName);
        Debug.Log("ajk " + filePath);
        AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(filePath);
        await WaitForCompletion(assetBundleCreateRequest);

        AssetBundle bundle = assetBundleCreateRequest.assetBundle;
        if (bundle == null)
        {
            Debug.LogError("Failed to load AssetBundle!");
            return;
        }

        string[] scenePaths = bundle.GetAllScenePaths();
        if (scenePaths.Length == 0 || sceneIndex >= scenePaths.Length)
        {
            Debug.LogError("Index not Found.");
            return;
        }

       if(PlayerPrefs.GetString("PlayschoolLanguageAudio") != "Hindi" &&
        PlayerPrefs.GetString("PlayschoolLanguageAudio") != "English")
       {
           Debug.Log($"PlayschoolLanguageAudio: {PlayerPrefs.GetString("PlayschoolLanguageAudio")}");
         Object audioAsset = await LoadAssetFromBundleAsync(audioPath, audioAssetName);
        if (audioAsset != null)
        {
            loadedAssets[audioAssetName] = audioAsset;
            Debug.Log($"Stored Asset: {audioAssetName}");
        }

        
       }

       // **Load Scene After Audio Is Stored**
        await LoadSceneAsync(scenePaths[sceneIndex]);
       
    }

    private static async Task WaitForCompletion(AsyncOperation operation)
    {
        while (!operation.isDone)
        {
            await Task.Yield();
        }
    }

    private static Task LoadSceneAsync(string scenePath)
    {
        var tcs = new TaskCompletionSource<bool>();
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(scenePath);
        asyncOperation.completed += _ => tcs.SetResult(true);
        return tcs.Task;
    }

    public static async Task<Object> LoadAssetFromBundleAsync(string audioPath, string assetName)
    {
     
    AssetBundle bundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(b => b.name == audioPath);
        bool after = false;
        if (after)
        {
            if (bundle == null)  //  Load only if not already in memory
            {


                var assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(audioPath);
                await WaitForCompletion(assetBundleCreateRequest);

                bundle = assetBundleCreateRequest.assetBundle;
                if (bundle == null)
                {
                    Debug.LogError("Failed to load AssetBundle: " + audioPath);
                    return null;
                }
            }
            else
            {
                Debug.Log($"AssetBundle '{audioPath}' is already loaded. Using existing bundle.");
            }
        }
    Object asset = bundle.LoadAsset(assetName);

    // if(asset == null){
    // return bundle.LoadAsset( PlayerPrefs.GetString("DefaultSOAudio" ));
    // }

    Debug.Log($"Loaded Asset: {assetName} {asset}");
    return asset;
    }

    public static T GetLoadedAsset<T>(string assetName) where T : Object
    {
        if (loadedAssets.TryGetValue(assetName, out Object asset))
        {
            Debug.Log($" Retrieved Asset: {assetName} -> {asset.name}");
            return asset as T;
        }
           Debug.Log($"Asset Not Found: {assetName}");
        return null;
        
        
     
    }
}
