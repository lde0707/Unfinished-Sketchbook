using UnityEngine;

namespace Unfin
{
    // Decorative only: fixed at the authored world position, behind gameplay terrain.
    [ExecuteAlways, DefaultExecutionOrder(100)]
    public sealed class RoomBackdropLayer : MonoBehaviour
    {
        public Camera trackingCamera;
        public Vector3 restPosition;
        public Vector4 roomBounds, uvRect;
        public float brightness=.52f, parallax=.065f;
        public bool flip;
        MeshRenderer target;
        void OnEnable(){Configure();}
        public void Configure()
        {
            target=GetComponent<MeshRenderer>();
            var props=new MaterialPropertyBlock();
            props.SetVector("_Room",roomBounds);props.SetVector("_UVRect",uvRect);
            props.SetFloat("_Brightness",brightness);props.SetFloat("_Flip",flip?1:0);
            target.SetPropertyBlock(props);
        }
        void LateUpdate(){PositionForCamera();}
        public void PositionForCamera()
        {
            // Keep the existing entry point/serialized data for scene and preview compatibility.
            // Camera position and the legacy parallax coefficient no longer affect placement.
            transform.position=restPosition;
        }
    }
}
