using UnityEngine;
namespace Unfin
{
    [DefaultExecutionOrder(-850)]
    public sealed class WorldViewport : MonoBehaviour
    {
        Camera world,clear;int width,height;
        public void Initialize(Camera value){world=value;var go=new GameObject("Letterbox clear");go.transform.SetParent(transform,false);clear=go.AddComponent<Camera>();clear.depth=world.depth-100;clear.clearFlags=CameraClearFlags.SolidColor;clear.backgroundColor=Color.black;clear.cullingMask=0;clear.useOcclusionCulling=false;Apply();}
        public static Rect Fit(int w,int h)
        {
            float aspect=(float)w/Mathf.Max(1,h),target=16f/9;
            return aspect>target?new Rect((1-target/aspect)*.5f,0,target/aspect,1):new Rect(0,(1-aspect/target)*.5f,1,aspect/target);
        }
        void Apply(){width=Screen.width;height=Screen.height;world.rect=Fit(width,height);world.aspect=16f/9;}
        void Update(){if(world!=null&&(width!=Screen.width||height!=Screen.height))Apply();}
    }
}
