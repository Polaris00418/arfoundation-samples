using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.XR.ARFoundation;
using Polaris.Unity.Base.Utilities.UI;

/// <summary>
/// Listens for touch events and performs an AR raycast from the screen touch point.
/// AR raycasts will only hit detected trackables like feature points and planes.
/// 
/// If a raycast hits a trackable, the <see cref="placedPrefab"/> is instantiated
/// and moved to the hit position.
/// </summary>

public class ProductPlacement : MonoBehaviour
{

    #region PUBLIC_MEMBERS

    public enum PlacementStates
    {
        NONE,
        PLACING,
        PLACED
    }

    [Header("Placement Controls")]
    public GameObject m_TranslationIndicator;
    public GameObject m_RotationIndicator;
    public RuntimeAnimatorController animatorController;
    public Transform modelParent;
    GameObject m_ModelObject;
    [Header("Placement Augmentation Size Range")]
    [Range(0.001f, 100.0f)]
    public float ProductSize = 0.65f;
    #endregion // PUBLIC_MEMBERS


    #region PRIVATE_MEMBERS
    PlacementStates placementState;
    public PlacementStates PlacementState
    {
        get { return placementState; }
        set { placementState = value; }
    }
    //Material[] chairMaterials, chairMaterialsTransparent;
    //Material ChairShadow, ChairShadowTransparent;
    //MeshRenderer chairRenderer;

    //[SerializeField]
    //MeshRenderer shadowRenderer;

    GroundPlaneUI m_GroundPlaneUI;
    Camera mainCamera;
       
    float m_PlacementAugmentationScale;
    Vector3 ProductScaleVector;
    #endregion // PRIVATE_MEMBERS

    private const float k_ModelRotation = 90f;

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    ARSessionOrigin m_SessionOrigin;

    #region MONOBEHAVIOUR_METHODS
    void Awake()
    {
        m_SessionOrigin = GameObject.FindObjectOfType<ARSessionOrigin>();
    }

    void Start()
    {       
        placementState = PlacementStates.NONE;
        m_GroundPlaneUI = FindObjectOfType<GroundPlaneUI>();
    }
       

    void Update()
    {
        UpdatePoisition();
        UpdateRotation();
    }

    void UpdatePoisition()
    {
        if (placementState == PlacementStates.NONE)
        {
            //auto hit through the center point of the screen to find the area to place 3d model
            if (m_SessionOrigin.Raycast(new Vector2(Screen.width / 2, Screen.height / 2), s_Hits, TrackableType.PlaneWithinPolygon))
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.
                var hitPose = s_Hits[0].pose;

                {
                    transform.position = hitPose.position;
                    // Compensate for the hitPose rotation facing away from the raycast (i.e. camera).
                    //transform.Rotate(0, k_ModelRotation, 0, Space.Self);
                    UtilityHelper.RotateTowardCamera(gameObject);
                    // Make Andy model a child of the anchor.
                    //transform.parent = anchor.transform;
                    UpdateState(PlacementStates.PLACING);
                }
            }
        }
        else if (placementState == PlacementStates.PLACING)
        {
            //****** 
            // Drag 3d model with 1 finger
            if (Input.touchCount != 1 /*|| (touch = Input.GetTouch(0)).phase != TouchPhase.Began*/)
            {
                return;
            }
            Touch touch = Input.GetTouch(0);

            if (TouchHandler.IsSingleFingerDragging && !m_GroundPlaneUI.IsCanvasButtonPressed() /* || Input.GetMouseButton(0)*/)
            {
                if (m_SessionOrigin.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
                {
                    // Raycast hits are sorted by distance, so the first one
                    // will be the closest hit.
                    var hitPose = s_Hits[0].pose;
                    transform.position = hitPose.position;
                    // Compensate for the hitPose rotation facing away from the raycast (i.e. camera).
                    //transform.Rotate(0, k_ModelRotation, 0, Space.Self);
                    UtilityHelper.RotateTowardCamera(gameObject);                       
                }
            }
            // Rotate 3d model with 2 finger
            //m_RotationIndicator.SetActive(Input.touchCount == 2);
        }
        else if (placementState == PlacementStates.PLACED)
        {
            Touch touch;
            if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
            {
                return;
            }
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(touch.position.x, touch.position.y, 0));
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                if (hitInfo.collider.transform.IsChildOf(transform) || hitInfo.collider.transform.Equals(transform))
                {
                    UpdateState(PlacementStates.PLACING);
                    return;
                }
            }
        }
    }
    public void UpdateState(PlacementStates state)
    {
        if (FurnitureARController.Instance.CurAugmentedPlacementObj == null
            || FurnitureARController.Instance.CurAugmentedPlacementObj == gameObject) {
            placementState = state;
            switch (state)
            {
                case PlacementStates.NONE:
                    FurnitureARController.Instance.CurAugmentedPlacementObj = gameObject;
                    FurnitureARController.Instance.GetComponent<ObjectViewer>().ShowOneObject("FirstPanel"); 
                    //PageViewer.Instance.ShowOneObject("FirstPanel");
                    GetComponent<TouchHandler>().enableRotation = false;
                    m_RotationIndicator.SetActive(false);
                    m_TranslationIndicator.SetActive(false);
                    break;
                case PlacementStates.PLACING:
                    FurnitureARController.Instance.CurAugmentedPlacementObj = gameObject;
                    GetComponentInChildren<Animator>().SetTrigger("bob");
                    FurnitureARController.Instance.GetComponent<ObjectViewer>().ShowOneObject("ModelPlacePage");
                    //PageViewer.Instance.ShowOneObject("ModelPlacePage");
                    GetComponent<TouchHandler>().enableRotation = true;
                    m_RotationIndicator.SetActive(true);
                    m_TranslationIndicator.SetActive(true);
                    break;
                case PlacementStates.PLACED:
                    GetComponentInChildren<Animator>().SetTrigger("land");
                    FurnitureARController.Instance.CurAugmentedPlacementObj = null;
                    FurnitureARController.Instance.GetComponent<ObjectViewer>().ShowOneObject("FirstPanel");
                    FurnitureARController.Instance.ShowRecogPointCloud(false);
                    GetComponent<TouchHandler>().enableRotation = false;
                    StartCoroutine(DisappearIndicator());
                    break;
                default:
                    break;
            }
        }
           
    }
    void UpdateRotation() {
        //identify vertical plane
        transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
    }
    IEnumerator DisappearIndicator() {
        m_RotationIndicator.SetActive(false);
        m_TranslationIndicator.SetActive(false);
        yield return new WaitForSeconds(0.5f);
    }
        
    #endregion // MONOBEHAVIOUR_METHODS


        
    public void Reset()
    {
        transform.position = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;
        transform.localScale = ProductScaleVector;
    }

    Vector3 buttomCenterPoint;
    public void ScaleIndicator(GameObject model)
    {
            
        // attach model to placement obj
        model.transform.parent = modelParent;
        model.transform.localScale = Vector3.one * ProductSize;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localPosition = Vector3.zero;
            
        // add collider on mesh renders
        foreach (MeshRenderer each in model.GetComponentsInChildren<MeshRenderer>())
        {
            each.gameObject.AddComponent<BoxCollider>();
        }
        // calc the max 
        Collider bigCol = new Collider();
        bigCol = GetComponentsInChildren<Collider>()[0];
        Vector3 minBoundPoint = bigCol.bounds.min;
        Vector3 maxBoundPoint = bigCol.bounds.max;
        Debug.Log("Collider" + bigCol.name);
        Debug.Log("BoundMin:" + minBoundPoint);
        Debug.Log("BoundMax:" + maxBoundPoint);
        foreach (BoxCollider each in model.GetComponentsInChildren<BoxCollider>())
        {
            if (each.bounds.min.x < minBoundPoint.x)
            {
                minBoundPoint.x = each.bounds.min.x;
            }
            if (each.bounds.min.y < minBoundPoint.y)
            {
                minBoundPoint.y = each.bounds.min.y;
            }
            if (each.bounds.min.z < minBoundPoint.z)
            {
                minBoundPoint.z = each.bounds.min.z;
            }
            if (each.bounds.max.x > maxBoundPoint.x)
            {
                maxBoundPoint.x = each.bounds.max.x;
            }
            if (each.bounds.max.y > maxBoundPoint.y)
            {
                maxBoundPoint.y = each.bounds.max.y;
            }
            if (each.bounds.max.z > maxBoundPoint.z)
            {
                maxBoundPoint.z = each.bounds.max.z;
            }
        }
        //bigCol.bounds.SetMinMax(minBoundPoint, maxBoundPoint);
        //bigCol.bounds.center = Vector3.zero;
        buttomCenterPoint = new Vector3(minBoundPoint.x / 2f + maxBoundPoint.x /2f, minBoundPoint.y, minBoundPoint.z / 2f + maxBoundPoint.z / 2f);
        buttomCenterPoint = buttomCenterPoint - model.transform.position;
        model.transform.parent.transform.Translate(-buttomCenterPoint, Space.Self);
        float range = Mathf.Max(maxBoundPoint.x - minBoundPoint.x, maxBoundPoint.z - minBoundPoint.z);
        //MessageBox.DisplayMessageBox("value", string.Format("Range:{0}", range), true, null);
        m_RotationIndicator.transform.localScale = range * Vector3.one;
        //m_TranslationIndicator.transform.localScale = range / m_PlacementAugmentationScale * 2 * Vector3.one;

        // animator
        model.AddComponent<Animator>();
        model.GetComponent<Animator>().runtimeAnimatorController = animatorController;

    }
        
    private GUIStyle _centeredStyle;
    //protected void OnGUI()
    //{
    //    Animator animator = modelParent.GetComponentInChildren<Animator>();
    //    if (animator == null)
    //    {
    //        return;
    //    }
    //    if (_centeredStyle == null)
    //    {
    //        _centeredStyle = GUI.skin.GetStyle("Label");
    //        _centeredStyle.alignment = TextAnchor.UpperCenter;
    //        _centeredStyle.fontSize = 50;
    //    }
    //    var centeredRect = new Rect(Screen.width / 2f - 250f, Screen.height - 300f, 500f, 200f);
    //    GUI.Label(centeredRect, string.Format("model  pos({0:F2}, {1:F2}, {2:F2})", animator.transform.position.x, animator.transform.position.y, animator.transform.position.z), _centeredStyle);

    //    var centeredRect2 = new Rect(Screen.width / 2f - 250f, Screen.height / 2f - 300f, 500f, 200f);
    //    GUI.Label(centeredRect2, string.Format("buttom pos({0:F2}, {1:F2}, {2:F2})", buttomCenterPoint.x,buttomCenterPoint.y, buttomCenterPoint.z), _centeredStyle);

    //    var centeredRect1 = new Rect(Screen.width / 2f - 250f, Screen.height / 2f - 600f, 500f, 200f);
    //    GUI.Label(centeredRect1, string.Format("parent pos({0:F2}, {1:F2}, {2:F2})", modelParent.transform.position.x, modelParent.transform.position.y, modelParent.transform.position.z), _centeredStyle);
    //}

}

