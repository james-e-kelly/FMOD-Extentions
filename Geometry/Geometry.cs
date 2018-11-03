using UnityEngine;

namespace FMODExtenstions.Geometry
{
    [AddComponentMenu("FMOD Studio/Low Level/Geometry Object")]
    public class Geometry : MonoBehaviour
    {
        public Polygon[] polygons = new Polygon[1];
        private FMOD.Geometry geometry;

        void Start()
        {
            geometry = ExtensionsManager.CreateGeometryObject(polygons.Length, ExtensionsUtils.GetTotalVerticesInPolygons(polygons));
            ExtensionsManager.AddPolygon(geometry, polygons);
        }

        void OnDestroy()
        {
            if (geometry.hasHandle())
            {
                geometry.release();
                geometry.clearHandle();
            }
        }

        void OnDrawGizmosSelected()
        {
            // Set all vertices to the position of the geometry object. This means it will take less time to moves the vertices to the correct position
            if (polygons.Length > 0)
            {
                for (int i = 0; i < polygons.Length; i++)
                {
                    if (polygons[i].vertices.Length > 0)
                    {
                        for (int y = 0; y < polygons[i].vertices.Length; y++)
                        {
                            if (polygons[i].vertices[y].position == Vector3.zero)
                            {
                                polygons[i].vertices[y].position = transform.position;
                            }

                            if (polygons[i].draw)
                            {
                                // Then, draw the position of the vectors for debugging
                                Gizmos.color = Settings.Instance.VerticesColor;
                                Gizmos.DrawSphere(polygons[i].vertices[y].position, 0.1f);

                                // If vertice is at end of array, it should draw a line from itself to the first vert
                                // Else, draw a line to the next vert along
                                Vector3 nextVert;
                                if (y < polygons[i].vertices.Length - 1)
                                {
                                    nextVert = polygons[i].vertices[y + 1].position;
                                }
                                else
                                {
                                    nextVert = polygons[i].vertices[0].position;
                                }

                                Gizmos.DrawLine(polygons[i].vertices[y].position, nextVert);
                            }
                        }
                    }
                }
            }
        }

        void OnValidate()
        {
            if (polygons.Length > 0)
            {
                for (int i = 0; i < polygons.Length; i++)
                {
                    polygons[i].DrawCube(transform.position);
                }
            }
        }
    }
}


