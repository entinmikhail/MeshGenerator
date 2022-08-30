using System;
using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [SerializeField] private int _meshLength;
    [SerializeField] private float _radius;
    [SerializeField] private Texture[] _1;
    [SerializeField] private Texture[] _1_n;
    [SerializeField] private GameObject _prefab;
    
    private List<Vector3> _vertices = new ();
    private List<int> _triangles = new ();
    private List<Vector2> _uv = new ();
    private Mesh _mesh;
    private Vector3 _offset = new Vector3(0, 0);   
    private int _subRow;
    private int _meshHeight;
    private float _height;
    private float _halfRadius;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int Layer1Tex = Shader.PropertyToID("_Layer1Tex");
    private List<GameObject> _pool = new ();
    
    private void Start()
    {
        _halfRadius = _radius / 2f;
        _meshHeight = _meshLength * 2;
        _height = Mathf.Sqrt(Mathf.Pow(_radius, 2) - Mathf.Pow(_halfRadius, 2));
        _offset = new Vector3(0, _halfRadius);
        Generate();
    }

    private void Generate()
    {   
        _mesh = new Mesh();
        _prefab.GetComponent<MeshFilter>().mesh = _mesh;
        _mesh.name = "Grid";
        _vertices = new List<Vector3>(_meshLength * _meshHeight * 3);
        _uv = new List<Vector2>(_meshLength * _meshHeight * 3);

        for (int i = 0, index = 0; i < _meshHeight + 2; i++)
        {
            _offset.x = 0;
            if (_subRow >= 5)
                _subRow = 1;
            
            var a = IsNeedToAddTriangle() ? _meshLength + 1 : _meshLength ;
            
            GetOffsetForTriangle(i, _height, _halfRadius, i + 1);
            for (int j = 0; j < a; j++, index++)
            {
                var newVertexes = OffsetTriangleByVector3(GetTriangleByRadius(_radius, IsNeedToRotate()), _offset);
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
                   var uv = new Vector2(vertex.x / (_meshLength * _height * 2), vertex.y / ((_meshHeight) * _height));
                   
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

        for (var i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                var go = Instantiate(_prefab, new Vector3((_meshLength) * _height * 2 * j, i * _radius * 2 * (_meshLength - 1)), Quaternion.identity);
            
                SetRendererTexture(go.GetComponent<MeshRenderer>(), MainTex, _1[i + j]);
                SetRendererTexture(go.GetComponent<MeshRenderer>(), Layer1Tex, _1_n[i + j]);
                go.SetActive(true);
                _pool.Add(go);
            }
            
        }
    }
    
    private bool IsNeedToAddTriangle()
    {
        var result = _subRow switch
        {
            4 => true,
            1 => true,
            0 => true,
            _ => false
        };
        
        return result;
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

    private void GetOffsetForTriangle(int i, float height, float halfRadius, int row)
    {
        switch (_subRow)
        {
            case 4:
                _offset.y += halfRadius;
                break;
            case 3:
                _offset.y += _radius;
                _offset.x = height;
                break;
            case 2:
                _offset.y += halfRadius;
                _offset.x = height;
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

    private Vector3[] GetTriangleByRadius(float radius, bool isRotate)
    {
        var hex = new Vector3[3];
        var halfRadius = radius / 2f;
        var height = Mathf.Sqrt(Mathf.Pow(radius, 2) - Mathf.Pow(halfRadius, 2));

        if (isRotate)
        {
            hex[0] = new Vector3(0, -radius);
            hex[1] = new Vector3(height, halfRadius);
            hex[2] = new Vector3(-height, halfRadius);
        }
        else
        {
            hex[0] = new Vector3(0, radius);
            hex[1] = new Vector3(height, -halfRadius);
            hex[2] = new Vector3(-height, -halfRadius); 
        }
        
        return hex;
    }
}