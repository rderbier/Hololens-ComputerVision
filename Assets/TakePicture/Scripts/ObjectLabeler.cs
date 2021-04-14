using System.Collections.Generic;
using CustomVison;
using UnityEngine;
using System;
using Microsoft.MixedReality.Toolkit.UI;


public class ObjectLabeler : MonoBehaviour
{
    private List<GameObject> _createdObjects = new List<GameObject>();

    [SerializeField]
    private GameObject markerOK;
    [SerializeField]
    private GameObject markerKO;

    [SerializeField]
    private GameObject buttonObj;

    [SerializeField]
    private GameObject _labelContainer;

    [SerializeField]
    private GameObject _debugObject;


    public virtual void LabelObjects(IList<RecognitionData> RecognitionData, 
        double pictureHorizontalAngleRadian, float heightFactor, Transform origin)
    {
        ClearLabels();
        Debug.Log(" pictureHorizontalAngleRadian =" + pictureHorizontalAngleRadian);
        Debug.Log("tan = "+Math.Tan(pictureHorizontalAngleRadian));
        Debug.Log("Forward  " + origin.forward.ToString("F2"));
        float widthAt1m = 2f * (float)Math.Tan(pictureHorizontalAngleRadian / 2f);
        float heightAt1m = widthAt1m * heightFactor;
        var topCorner = origin.position + origin.forward -
                         origin.right * (widthAt1m / 2f) +
                        origin.up * (heightAt1m / 2f);
        Debug.Log(" widthAt1m =" + widthAt1m + " heightAt1m = " + heightAt1m + " topCorner =" + topCorner.ToString("F2"));
        foreach (var rec in RecognitionData)
        {
            Debug.Log(" box x =" + rec.boundingBox.x + " y=" + rec.boundingBox.y + " width = " + rec.boundingBox.width +" height = "+ rec.boundingBox.height);
            float x = widthAt1m * (rec.boundingBox.x + rec.boundingBox.width / 2f);
            float y = heightAt1m * (rec.boundingBox.y + rec.boundingBox.height / 2);
            
            var recognizedPos = topCorner + origin.right * x - origin.up * y ;
            Debug.Log(" Label x=" + x + " y=" + y + " position =" + recognizedPos.ToString("F2"));
            var marker = rec.marker == "ko" ? markerKO : markerOK;
//#if UNITY_EDITOR
//            Debug.Log(" Add marker in Editor");
//             _createdObjects.Add(CreateLabel(rec.text, recognizedPos, marker));
//#else
           // _createdObjects.Add(CreateLabel(rec.text, recognizedPos, marker));

            var labelPos = DoRaycastOnSpatialMap(origin, recognizedPos);
            if (labelPos != null)
            {
                Debug.Log("ray hit on layer");
                _createdObjects.Add(CreateLabel(rec.text, labelPos.Value, marker));
                _createdObjects.Add(CreateTool("Instructions", labelPos.Value + Vector3.up * 0.15f + Vector3.right * 0.01f, buttonObj));
                _createdObjects.Add(CreateTool("raise Case", labelPos.Value + Vector3.up * 0.15f + Vector3.right * 0.05f, buttonObj));
                _createdObjects.Add(CreateTool("show Stats", labelPos.Value + Vector3.up * 0.15f + Vector3.right * 0.09f, buttonObj));
                _createdObjects.Add(CreateTool("Help", labelPos.Value + Vector3.up * 0.15f + Vector3.right * 0.13f, buttonObj));
            } else
            {
                Debug.Log("no hit on layer");
                _createdObjects.Add(CreateLabel(rec.text, recognizedPos, marker));
                _createdObjects.Add(CreateTool("Instructions", recognizedPos + Vector3.up * 0.15f + Vector3.right * 0.01f, buttonObj));
                _createdObjects.Add(CreateTool("raise Case", recognizedPos + Vector3.up * 0.15f + Vector3.right * 0.05f, buttonObj));
                _createdObjects.Add(CreateTool("show Stats", recognizedPos + Vector3.up * 0.15f + Vector3.right * 0.09f, buttonObj));
                _createdObjects.Add(CreateTool("Help", recognizedPos + Vector3.up * 0.15f + Vector3.right * 0.13f, buttonObj));
            }
//#endif
        }

        if (_debugObject != null)
        {
             _debugObject.SetActive(false);
        }

       // Destroy(cameraTransform.gameObject);
    }

    private Vector3? DoRaycastOnSpatialMap(Transform cameraTransform, Vector3 recognitionCenterPos)
    {
        RaycastHit hitInfo;
        var layer = LayerMask.NameToLayer("Spatial Awareness");
        if (Physics.Raycast(cameraTransform.position, (recognitionCenterPos - cameraTransform.position), out hitInfo, 10))
        {
            return hitInfo.point;
        }
        return null;
    }

    private void ClearLabels()
    {
        foreach (var label in _createdObjects)
        {
            Destroy(label);
        }
        _createdObjects.Clear();
    }

    private GameObject CreateLabel(string text, Vector3 location, GameObject marker)
    {
        var labelObject = Instantiate(marker);
        labelObject.transform.position = location;
        var toolTip = labelObject.GetComponentInChildren<ToolTip>() as ToolTip;
        if (toolTip != null)
        {
            toolTip.ToolTipText = text;
            Debug.Log("Set Tooltip text ");
        } else
        {
            Debug.Log("no Tooltip");
        }
        //var toolTip = labelObject.GetComponent<ToolTip>();
        //toolTip.ShowOutline = false;
        //toolTip.ShowBackground = true;
        //toolTip.ToolTipText = text;
        //toolTip.transform.position = location + Vector3.up * 0.2f;
        //toolTip.transform.parent = _labelContainer.transform;
        //toolTip.AttachPointPosition = location;
        //toolTip.ContentParentTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //var connector = toolTip.GetComponent<ToolTipConnector>();
        //connector.PivotDirectionOrient = ConnectorOrientType.OrientToCamera;
        //connector.Target = labelObject;
        return labelObject;
    }

    private GameObject CreateTool(string text, Vector3 location, GameObject btn)
    {
        var btnObject = Instantiate(btn);
        btnObject.transform.position = location;
        //btnObject.transform.LookAt(cameraTransform);
        btnObject.transform.rotation = Quaternion.Euler(new Vector3(10, 0, 0));
        var tool = btnObject.GetComponentInChildren<ButtonConfigHelper>() as ButtonConfigHelper;
        if (tool != null)
        {
            tool.MainLabelText = text;
            Debug.Log("Set Tool text ");
        }
        else
        {
            Debug.Log("no Tool btn");
        }
        return btnObject;
    }
}

