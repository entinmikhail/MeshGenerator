using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [SerializeField] private int _xSize, _ySize;
    [SerializeField] private float _radius;
    [SerializeField] private Texture[] _1;
    [SerializeField] private Texture[] _1_n;
    [SerializeField] private GameObject _prefab;

    // private readonly Vector2 _off = new Vector2(0.87f, 0.5f);
    private readonly Vector2 _off = new Vector2(0f, 0f);
    private List<Vector3> _vertices = new ();
    private List<int> _triangles = new ();
    private List<Vector2> _uv = new ();
    private Mesh _mesh;
    private Vector3 _offset;   
    private int _subRow;
    private float _height;
    private float _halfRadius;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int Layer1Tex = Shader.PropertyToID("_Layer1Tex");
    
    private void Start()
    {
        _halfRadius = _radius / 2f;
        _height = Mathf.Sqrt(Mathf.Pow(_radius, 2) - Mathf.Pow(_halfRadius, 2));
        // _offset = new Vector3(_off.x * _radius, _off.y * _radius);

        Generate();
    }

    private void Generate()
    {   
        _mesh = new Mesh();
        _prefab.GetComponent<MeshFilter>().mesh = _mesh;
        _mesh.name = "Grid";
        _vertices = new List<Vector3>(_xSize * _ySize * 3);
        _uv = new List<Vector2>(_xSize * _ySize * 3);
        
        
        for (int i = 0, index = 0; i < _ySize; i++)
        {
            _offset.x = _off.x * _radius;
            
            RefreshForTriangle();

            for (int j = 0; j < _xSize; j++, index++)
            {
                if (_offset.x < 0 || _offset.y < 0)
                {
                    Debug.LogError(_offset);
                }
                
                var newVertexes = OffsetTriangleByVector3(GetTriangleByRadius(IsNeedToRotate()), _offset);
                _vertices.AddRange(newVertexes);

                foreach (var vertex in newVertexes)
                {
                    if (vertex.x < 0 || vertex.y < 0)
                    {
                        Debug.LogError(vertex);
                    }
                }
                _triangles.AddRange(GetTrianglesByVertex(newVertexes, index, IsNeedToRotate()));

                foreach (var vertex in newVertexes)
                {
                   var uv = new Vector2(vertex.x / _xSize, vertex.y / (_ySize / 2f));
                   
                    _uv.Add(uv);

                }

                _offset += new Vector3(_height * 2, 0);
            }
        }

        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();
        _mesh.uv = _uv.ToArray();
        _mesh.RecalculateNormals();
        
        _prefab.SetActive(false);

        for (var i = 0; i < 3; i++)
        {
            var go = Instantiate(_prefab, new Vector3(_xSize * _height * 2 * i, 0), Quaternion.identity);
            
            SetRendererTexture(go.GetComponent<MeshRenderer>(), MainTex, _1[i]);
            SetRendererTexture(go.GetComponent<MeshRenderer>(), Layer1Tex, _1_n[i]);
            go.SetActive(true);
        }
    }
    
    public static void SetRendererTexture(Renderer renderer, int nameId, Texture texture)
    {
        MaterialPropertyBlock colorProperty = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(colorProperty);
        colorProperty.SetTexture(nameId, texture);
        renderer.SetPropertyBlock(colorProperty);
    }
    
    private bool IsNeedToRotate()
    {
        var result = _subRow switch
        {
            4 => false,
            3 => true,
            2 => false,
            1 => true,
            _ => true
        };
        
        return result;
    }

    private void RefreshForTriangle()
    {
        if (_subRow >= 5) 
            _subRow = 1;


        switch (_subRow)
        {
            case 4:
                _offset.y += _halfRadius;
                break;
            case 3:
                _offset.y += _radius;
                _offset.x = _height + _off.x * _radius;
                break;
            case 2:
                _offset.y += _halfRadius;
                _offset.x = _height + _off.x * _radius;
                break;
            case 1:
                _offset.y += _radius;
                break;
            case 0:
                _subRow++;
                break;
        }

        _subRow++;

    }

    private Vector3[] OffsetTriangleByVector3(Vector3[] triangleVertexes, Vector3 offset)
    {
        var hex = new Vector3[3];

        for (int i = 0; i < hex.Length; i++)
            hex[i] = triangleVertexes[i] + offset;
        
        return hex;
    }

    private int[] GetTrianglesByVertex(Vector3[] vertexPoints, int index, bool isRevert)
    {
        index *= 3;
        var triangles = new int[3];

        if (isRevert)
        {
            triangles[0] = 2 + index;
            triangles[1] = 1 + index;
            triangles[2] = 0 + index;
        }
        else
        {
            triangles[0] = 0 + index;
            triangles[1] = 1 + index;
            triangles[2] = 2 + index;
        }
        
        return triangles;
    }

    private Vector3[] GetTriangleByRadius(bool isRotate)
    {
        var hex = new Vector3[3];

        if (isRotate)
        {
            hex[0] = new Vector3(0, -_radius);
            hex[1] = new Vector3(_height, _halfRadius);
            hex[2] = new Vector3(-_height, _halfRadius);
        }
        else
        {
            hex[0] = new Vector3(0, _radius);
            hex[1] = new Vector3(_height, -_halfRadius);
            hex[2] = new Vector3(-_height, -_halfRadius); 
        }
        
        return hex;
    }

    private Vector3 OldGetOffsetForTriangle(int i, float height, float halfRadius, int row)
    {
        Debug.Log(row % 3 == 0);
        Debug.Log(row);
        if (row % 4 == 0)
            return new Vector3(0, _radius * (row - 2) );
        if (row % 3 == 0)
            return new Vector3(height, _radius * (row-1) - halfRadius);
        if (row % 2 == 0)
            return new Vector3(height, _radius *(row-1) - halfRadius);

        return new Vector3(0, _radius * (row-1));
    }
    
    private Vector3 GetOffsetForHex(int i, float height, float halfRadius)
    {
        Vector3 offset;
        if (i % 2 == 1)
            offset = new Vector3(height * i, (_radius + halfRadius) * i);
        else
            offset = new Vector3(0, (_radius + halfRadius) * i);
        return offset;
    }

    private Vector3[] OffsetHexByVector3(Vector3[] hexVertexes, Vector3 offset)
    {
        var hex = new Vector3[6];

        for (int i = 0; i < hex.Length; i++)
            hex[i] = hexVertexes[i] + offset;
        
        return hex;
    }

    private int[] GetHexByVertex(Vector3[] vertexPoints, int index)
    {
        index *= 6;
        var triangles = new int[12];
        triangles[0] = 5 + index;
        triangles[1] = 0 + index;
        triangles[2] = 1 + index;
        
        triangles[3] = 1 + index;
        triangles[4] = 2 + index;
        triangles[5] = 5 + index;
        
        triangles[6] = 4 + index;
        triangles[7] = 5 + index;
        triangles[8] = 2 + index;
        
        triangles[9] = 2 + index;
        triangles[10] = 3 + index;
        triangles[11] = 4 + index;

        return triangles;
    }


    private Vector3[] GetHexByRadius(int radius)
    {
        var hex = new Vector3[6];
        var halfRadius = radius / 2f;
        var height = Mathf.Sqrt(Mathf.Pow(radius, 2) - Mathf.Pow(halfRadius, 2));
        
        hex[0] = new Vector3(0, radius);
        hex[1] = new Vector3(height, halfRadius);
        hex[2] = new Vector3(height, -halfRadius);
        hex[3] = new Vector3(0, -radius);
        hex[4] = new Vector3(-height, -halfRadius);
        hex[5] = new Vector3(-height, halfRadius);
        
        return hex;
    }
    
    // private void OnDrawGizmos()
    // {
    //     // return;
    //     if (_vertices == null)
    //     {
    //         return;
    //     }
    //     Gizmos.color = Color.red;
    //     foreach (var t in _vertices)
    //     {
    //         Gizmos.DrawSphere(t, 0.2f);
    //     }
    // }
    
    // private void Generate()
    // {
    //     _mesh = new Mesh();
    //     GetComponent<MeshFilter>().mesh = _mesh;
    //     _mesh.name = "Grid";
    //
    //     _vertices = new Vector3[(_xSize + 1) * (_ySize + 1)];
    //     Vector2[] uvs = new Vector2[_vertices.Length];
    //     Vector4[] tangents = new Vector4[_vertices.Length];
    //     Vector4 tangent = new Vector4(1f, 0f, 0f, -1f);
    //     for (int i = 0, y = 0; y <= _ySize; y++)
    //     {
    //         for (int x = 0; x <= _xSize; x++, i++)
    //         {
    //             _vertices[i] = new Vector3(x, y);
    //             uvs[i] = new Vector2((float)x / _xSize, (float)y / _ySize);
    //             tangents[i] = tangent;
    //         }
    //     }
    //     _mesh.vertices = _vertices;
    //     _mesh.uv = uvs;
    //     _mesh.tangents = tangents;
    //
    //     int[] triangles = new int[_xSize * _ySize * 6];
    //     int ti = 0, vi = 0;
    //     for (int y = 0; y < _ySize; y++, vi++)
    //     {
    //         for (int x = 0; x < _xSize; x++, ti += 6, vi++)
    //         {
    //             triangles[ti] = vi;
    //             triangles[ti + 1] = triangles[ti + 4] = vi + _xSize + 1;
    //             triangles[ti + 2] = triangles[ti + 3] = vi + 1;
    //             triangles[ti + 5] = vi + _xSize + 2;
    //         }
    //     }
    //             
    //
    //     _mesh.triangles = triangles;
    //     _mesh.RecalculateNormals();
    // }
}