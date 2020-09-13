//FROM BoundingFrustum class of https://gist.github.com/StagPoint/4d8ca93923f66ad60ce480124c0d5092
using System;

namespace UnityEngine.NetLibrary
{
    [Serializable]
    public class NetCPUCamera
    {
        /// <summary>
        /// The number of planes in the frustum.
        /// </summary>
        public const int PlaneCount = 6;

        /// <summary>
        /// The number of corner points in the frustum.
        /// </summary>
        public const int CornerCount = 8;

        /// <summary>
        /// Returns the current position of the frustum
        /// </summary>

        /// <summary>
        /// Ordering: [0] = Far Bottom Left, [1] = Far Top Left, [2] = Far Top Right, [3] = Far Bottom Right, 
        /// [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right
        /// </summary>
        private readonly Vector3[] _corners = new Vector3[CornerCount];

        /// <summary>
        /// Defines the set of planes that bound the camera's frustum. All plane normals point to the inside of the 
        /// frustum.
        /// Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
        /// </summary>
        public readonly Plane[] Planes = new Plane[PlaneCount];

        public void UpdateOrthographic(Transform camera, float orthographicSize, float aspect, float farClipPlane, float nearClipPlane)
        {
            var position = camera.position;
            var orientation = camera.rotation;

            //Position = position;
            var forward = orientation * Vector3.forward;

            var right = orientation * Vector3.right * orthographicSize * aspect;
            var up = orientation * Vector3.up * orthographicSize;

            // CORNERS:
            // [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
            // [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

            _corners[0] = position + forward * farClipPlane - up - right;
            _corners[1] = position + forward * farClipPlane + up - right;
            _corners[2] = position + forward * farClipPlane + up + right;
            _corners[3] = position + forward * farClipPlane - up + right;
            _corners[4] = position + forward * nearClipPlane - up - right;
            _corners[5] = position + forward * nearClipPlane + up - right;
            _corners[6] = position + forward * nearClipPlane + up + right;
            _corners[7] = position + forward * nearClipPlane - up + right;

            // PLANES:
            // Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far

            Planes[0] = new Plane(_corners[4], _corners[1], _corners[0]);
            Planes[1] = new Plane(_corners[6], _corners[3], _corners[2]);
            Planes[2] = new Plane(_corners[7], _corners[0], _corners[3]);
            Planes[3] = new Plane(_corners[5], _corners[2], _corners[1]);
            Planes[4] = new Plane(forward, position + forward * nearClipPlane);
            Planes[5] = new Plane(-forward, position + forward * farClipPlane);

            /*for (int i = 0; i < PlaneCount; i++)
            {
                var plane = _planes[i];
                var normal = plane.normal;

                _absNormals[i] = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
                _planeNormal[i] = normal;
                _planeDistance[i] = plane.distance;
            }*/
        }

        public void UpdatePespective(Transform camera, float fov, float aspect, float farClipPlane, float nearClipPlane)
        {
            var position = camera.position;
            var orientation = camera.rotation;

            var forward = orientation * Vector3.forward;

            //FRUSTUM CORNERS START
            float fovWHalf = fov * 0.5f;

            Vector3 toRight = Vector3.right * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * aspect;
            Vector3 toTop = Vector3.up * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

            Vector3 topLeft = (Vector3.forward - toRight + toTop);
            float camScale = topLeft.magnitude * farClipPlane;

            topLeft.Normalize();
            topLeft *= camScale;

            Vector3 topRight = (Vector3.forward + toRight + toTop);
            topRight.Normalize();
            topRight *= camScale;

            Vector3 bottomRight = (Vector3.forward + toRight - toTop);
            bottomRight.Normalize();
            bottomRight *= camScale;

            Vector3 bottomLeft = (Vector3.forward - toRight - toTop);
            bottomLeft.Normalize();
            bottomLeft *= camScale;

            _corners[0] = position + orientation * bottomLeft;
            _corners[1] = position + orientation * topLeft;
            _corners[2] = position + orientation * topRight;
            _corners[3] = position + orientation * bottomRight;

            topLeft = (Vector3.forward - toRight + toTop);
            camScale = topLeft.magnitude * nearClipPlane;

            topLeft.Normalize();
            topLeft *= camScale;

            topRight = (Vector3.forward + toRight + toTop);
            topRight.Normalize();
            topRight *= camScale;

            bottomRight = (Vector3.forward + toRight - toTop);
            bottomRight.Normalize();
            bottomRight *= camScale;

            bottomLeft = (Vector3.forward - toRight - toTop);
            bottomLeft.Normalize();
            bottomLeft *= camScale;

            _corners[4] = position + orientation * bottomLeft;
            _corners[5] = position + orientation * topLeft;
            _corners[6] = position + orientation * topRight;
            _corners[7] = position + orientation * bottomRight;
            //FRUSTUM CORNERS END

            Planes[0] = new Plane(_corners[4], _corners[1], _corners[0]);
            Planes[1] = new Plane(_corners[6], _corners[3], _corners[2]);
            Planes[2] = new Plane(_corners[7], _corners[0], _corners[3]);
            Planes[3] = new Plane(_corners[5], _corners[2], _corners[1]);
            Planes[4] = new Plane(forward, position + forward * nearClipPlane);
            Planes[5] = new Plane(-forward, position + forward * farClipPlane);
        }
    }
}