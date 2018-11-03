using UnityEngine;

namespace FMODExtenstions.Geometry
{
    /// <summary>
    /// Used as a face on an FMOD Geometry object
    /// </summary>
    [System.Serializable]
    public class Polygon
    {
        public Vertex[] vertices = new Vertex[1];

        [Range(0, 1)]
        public float directOcclusion = 0f;
        [Range(0, 1)]
        public float reverbOcclusion = 0f;
        public bool doubleSided = false;

        public bool isRectangle = false;
        public float width = 2f;
        public float height = 1f;

        public bool draw = false;

        public Vector3 rotation = Vector3.zero;

        [HideInInspector]
        public int polygonIndex = 0;

        /// <summary>
        /// Debug our positions if the polygon is a rectangle or square
        /// </summary>
        /// <param name="geometryPosition">Geometry position.</param>
        public void DrawCube (Vector3 geometryPosition)
        {
            if (isRectangle)
            {
                if (vertices.Length != 4)
                {
                    vertices = new Vertex[4];
                }

                // 0 = Top Left, 1 = Top Right, 2= Bottom Right, 3 = Bottom Left

                // Move all positions to origin
                Vector3 topLeft = geometryPosition;
                Vector3 topRight = geometryPosition;
                Vector3 bottomRight = geometryPosition;
                Vector3 bottomLeft = geometryPosition;

                // Set each vertex depending on width and height
                topLeft.x -= width;
                topLeft.y += height;

                topRight.x += width;
                topRight.y += height;

                bottomRight.x += width;
                bottomRight.y -= height;

                bottomLeft.x -= width;
                bottomLeft.y -= height;

                // Allow vertices to move with rotation
                topLeft = ExtensionsUtils.RotateAroundPivot(topLeft, geometryPosition, rotation);
                topRight = ExtensionsUtils.RotateAroundPivot(topRight, geometryPosition, rotation);
                bottomRight = ExtensionsUtils.RotateAroundPivot(bottomRight, geometryPosition, rotation);
                bottomLeft = ExtensionsUtils.RotateAroundPivot(bottomLeft, geometryPosition, rotation);

                // Set the new positions we just worked out to be the one on the polygon
                vertices[0].position = topLeft;
                vertices[1].position = topRight;
                vertices[2].position = bottomRight;
                vertices[3].position = bottomLeft;
            }
        }
    }
}


