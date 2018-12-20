//-----------------------------------------------------------------------
// <copyright file="FunitureARController.cs" company="FantasyDigital">
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;
using TriLib;
using Polaris.Unity.Base.Utilities.UI;

public class FurnitureARController : MonoBehaviour
{
    private bool isBusy = false;
    private ObjectLoadedHandle objectLoaded = null;
    private GameObject curAugmentedPlacementObj;

    public static FurnitureARController Instance;
    public GameObject CurAugmentedPlacementObj
    {
        get { return curAugmentedPlacementObj; }
        set { curAugmentedPlacementObj = value; }
    }
    public GameObject m_PlacedPrefab;

    public void FinishPlacingModel() {
        curAugmentedPlacementObj.GetComponent<ProductPlacement>().UpdateState(ProductPlacement.PlacementStates.PLACED);
    }

    public void RemoveTyingModel() {
        GameObject.DestroyImmediate(curAugmentedPlacementObj);
        curAugmentedPlacementObj = null;
    }

    // load model with code
    public void LoadModelWithCode(Text code)
    {
        if (isBusy) return;
        isBusy = true;
        StartCoroutine(LoadModelode(code.text));
    }

    public void LoadModelWithBundle(GameObject bundle)
    {
        curAugmentedPlacementObj = Instantiate(m_PlacedPrefab, new Vector3(0, 100, 0), Quaternion.identity);
            
        ///GameObject model = GameObject.Instantiate(bundle);
        GameObject model = bundle;
        curAugmentedPlacementObj.GetComponent<ProductPlacement>().ScaleIndicator(model);
        isBusy = false;
    }

        
    public GameObject poingCloud;
    public GameObject planeGenerator;
    public void ShowRecogPointCloud(bool flag) {
        poingCloud.SetActive(flag);
        planeGenerator.SetActive(flag);
    }
    

    private string GetBundleNameFromURL(string url)
    {
        string[] tokens = url.Split('/');
        return tokens[tokens.Length - 1];
    }
    IEnumerator LoadModelode(string code)
    {
        Item _item = new Item();
        string strUrl = "http://agselling.com/arcore/json.php?id=" + code;
        Debug.Log(strUrl);
        WWW www = new WWW(strUrl);
        yield return www;
        if (string.IsNullOrEmpty(www.error))
        {
            string correct = www.text.Remove(www.text.Length - 4, 1);
            JsonData json = JsonMapper.ToObject(correct);
            _item.url = json["url"].ToString();
            Debug.Log("url:" + _item.url);
        }
        else
        {
            Debug.Log("error");
            //_ShowAndroidToastMessage("controlla la connessione di rete");
            MessageBox.DisplayMessageBox("Error", "Poor Network Connection!", false, null);
            FurnitureARController.Instance.gameObject.GetComponent<ObjectViewer>().ShowOneObject("FirstPanel");
            yield break;
        }
        if (_item.url == "error")
        {
            //_ShowAndroidToastMessage("codice non VALIDO!!!");
            MessageBox.DisplayMessageBox("Error", "Invalid Code!", true, null);
            FurnitureARController.Instance.GetComponent<ObjectViewer>().ShowOneObject("FirstPanel");
            yield break;
        }
        else
        {
            //_item.URL = "https://agselling.com/arcore/output/AssetBundles/Android/sofa-collection";        
            //_item.URL = "https://agselling.com/arcore/output/AssetBundles/Android/andy";
            //_item.URL = "https://agselling.com/arcore/output/AssetBundles/Android/cucina";
            string bundleName = GetBundleNameFromURL(_item.url);
            //objectLoaded = null;
            //objectLoaded += LoadModelWithBundle;
            //GetComponent<MyLoadAssets>().DoLoad(bundleName, bundleName);
            GetComponent<MyAssetDownloader>().StartDownload(_item.url, "fbx");
            FurnitureARController.Instance.gameObject.GetComponent<ObjectViewer>().HideAllObjects();

        }

    }

    void Awake()
    {
        Instance = this;
        ShowRecogPointCloud(false);
    }
}

