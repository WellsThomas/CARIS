using System;
using System.Collections;
using Communication;
using Communication.ActionPackage;
using Interaction.Drawable;
using JetBrains.Annotations;
using Packages.Serializable;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using Random = System.Random;

public class FreezeFrame : MonoBehaviour
{
    [SerializeField]
    private RawImage image;

    [SerializeField] private GameObject canvas;
    [SerializeField] private Drawable drawable;

    [CanBeNull] private Texture2D _currentTexture = null;

    private static FreezeFrame _freezeFrameManager;

    public static FreezeFrame Get()
    {
        return _freezeFrameManager;
    }

    public void Start()
    {
        _freezeFrameManager = this;
    }

    private IEnumerator CatchFrame()
    {
        yield return new WaitForEndOfFrame();
        
        var texture = new Texture2D(Screen.width,Screen.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);
        texture.Apply();
        
        SetImageAsFreezeFrame(texture);
    }

    public void Freeze()
    {
        ChangeToFreezeCanvas();
        
        // Catch frame
        StartCoroutine(CatchFrame());
        
        // Setup Drawing
        SetupNewDrawing();
    }

    private void SetupNewDrawing()
    {
        var random = new Random();
        var newID = random.Next(Int32.MinValue, Int32.MaxValue);
        drawable.SetDrawingID(newID);
    }

    private void SetImageAsFreezeFrame(Texture2D texture)
    {
        image.texture = texture;
        _currentTexture = texture;
        SetActive(true);
    }

    private void ChangeToFreezeCanvas()
    {
        // Hide canvas
        canvas.SetActive(false);
        
        // Set height
        image.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
    }

    public void DistributeFrame()
    {
        if (_currentTexture == null) return;
        var imgBytes = ImageConversion.EncodeToJPG(_currentTexture);
        var drawingSegment = drawable.GetDrawing();
        var action = new DistributeFreezeFrameAction(imgBytes, _currentTexture.height, _currentTexture.width, drawable.GetID(), drawingSegment);
        Stringifier.GetStringifier().StringifyAndForward<DistributeFreezeFrameAction>(action, TypeOfPackage.DistributeFreezeFrame, true);
    }

    public void DisplayForeignFrame(DistributeFreezeFrameAction action)
    {
        var texture = new Texture2D (action.Width, action.Height);
        texture.LoadImage(action.Data);
        ChangeToFreezeCanvas();
        SetImageAsFreezeFrame(texture);
        drawable.SetDrawingID(action.DrawingID);
        drawable.ApplyDrawing(action.DrawingSegments);
    }

    public void CloseFreezeFrame()
    {
        SetActive(false);
    }


    private void SetActive(bool active)
    {
        image.transform.parent.gameObject.SetActive(active);
        canvas.SetActive(!active);
    }
}
