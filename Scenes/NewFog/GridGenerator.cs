using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [SerializeField] private int _meshLength;
    [SerializeField] private float _radius;
    [SerializeField] private Texture[] _1;
    [SerializeField] private Texture[] _1_n;
    [SerializeField] private Texture _fog;
    [SerializeField] private GameObject _prefab;
    [SerializeField] private GameObject _quad;
    [SerializeField] private float _size = 5f;
    [SerializeField] private int _indexToColor;
    [SerializeField] private Color _color;
    
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
    private static readonly int Layer2Tex = Shader.PropertyToID("_Layer2Tex");
    private List<GameObject> _pool = new ();
    private Dictionary<int, Triangle> _trianglesByIndex = new();
    private Dictionary<int, List<int>> _meshIndexesByGlobalVertexIndex = new();
    private Dictionary<int, Mesh> _meshByMeshIndex = new();
    private Dictionary<int, Dictionary<int, List<int>>> _meshIndexAndLocalVertexIndexPairByGlobalVertexIndex = new();
    private Dictionary<Vector3, Dictionary<int, List<int>>> _meshIndexAndLocalVertexIndexPairByPosition = new();
    private Dictionary<int, Vector3> _vertexPositionByHexIndex = new();

    private void Awake()
    {
        _halfRadius = _radius / 2f;
        _meshHeight = _meshLength * 2;
        _height = Mathf.Sqrt(Mathf.Pow(_radius, 2) - Mathf.Pow(_halfRadius, 2));
        
        for (var i = 0; i < 6; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                var quadCenter = new Vector3(_size / 2 + j * _size, _size / 2 + i * _size);
                var halfSize = _size / 2;

                var a = new Vector3(quadCenter.x - halfSize, quadCenter.y + halfSize);
                var b = new Vector3(quadCenter.x + halfSize, quadCenter.y + halfSize);
                var c = new Vector3(quadCenter.x + halfSize, quadCenter.y - halfSize);
                var d = new Vector3(quadCenter.x - halfSize, quadCenter.y - halfSize);
                
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _vertices.Add(d);
            }
        }
    }

    private void Start()
    {
        GlobalGenerate();
        
        for (var i = 6; i > 0; i--)
        {
            for (var j = 0; j < 8; j++)
            {
                var spriteIndex = j + (8 * (6 - i));

                var quadCenter = new Vector3(_size / 2 + j * _size, _size / 2 + (i - 1) * _size);
                var halfSize = _size / 2;
                var go = Generate(quadCenter, _size, spriteIndex);
                var render = go.GetComponent<MeshRenderer>();
                
                SetRendererTexture(render, MainTex, _1[spriteIndex]);
                SetRendererTexture(render, Layer1Tex, _1_n[spriteIndex]);
                SetRendererTexture(render, Layer2Tex, _fog);
            }
        }

        var k = 0;
        foreach (var kvp in _meshIndexAndLocalVertexIndexPairByPosition)
        {
             _vertexPositionByHexIndex.Add(k, kvp.Key);
             k++;
        }
    }

    private void Update()
    {
        // _indexToColor++;

        foreach (var kvp in _meshIndexAndLocalVertexIndexPairByPosition[_vertexPositionByHexIndex[_indexToColor]])
        {
            if (_meshByMeshIndex.TryGetValue(kvp.Key, out var mesh))
            {
                foreach (var index in kvp.Value)
                {
                    var colors = mesh.colors;
                    colors[index] = _color;
                    mesh.colors = colors;
                }
            }
        }
        
        // if (_meshIndexAndLocalVertexIndexPairByGlobalVertexIndex.TryGetValue(_indexToColor, out var dict))
        // {
        //     foreach (var meshIndexPair in dict)
        //     {
        //         if (_meshByMeshIndex.TryGetValue(meshIndexPair.Key, out var mesh))
        //         {
        //             foreach (var index in meshIndexPair.Value)
        //             {
        //                 var colors = mesh.colors;
        //                 colors[index] = Color.red;
        //                 mesh.colors = colors;
        //             
        //                 // mesh.colors[meshIndexPair.Value] = Color.red;
        //             
        //                 Debug.Log(mesh.vertices[index]);
        //                 Debug.Log(mesh.uv[index]);
        //                 Debug.Log(mesh.colors[index]);
        //                 Debug.Log(Color.red);
        //             }
        //         }
        //     }
        // }
    }

    private void OnDrawGizmos()
    {
        if (_vertices == null)
        {
            return;
        }
        Gizmos.color = Color.red;
        for (int i = 0; i < _vertices.Count; i++)
        {
            Gizmos.DrawSphere(_vertices[i], 0.2f);
        }
    }
    
    private GameObject Generate(Vector3 position, float size, int meshIndex)
    {
        var mesh = new Mesh();
        var go = Instantiate(_prefab);
        go.GetComponent<MeshFilter>().mesh = mesh;
        mesh.name = "Grid";
        
        _meshByMeshIndex.TryAdd(meshIndex, mesh);

        var vertices = new List<Vector3>(_meshLength * _meshHeight * 3);
        var triangles = new List<int>(_meshLength * _meshHeight * 3);
        var uv = new List<Vector2>(_meshLength * _meshHeight * 3);
        var colors = new List<Color>(_meshLength * _meshHeight * 3);

        var index = 0;
        foreach (var triangleByIndex in _trianglesByIndex)
        {
            foreach (var vertex in triangleByIndex.Value.Vertexes)
            {
                if (IsPointInQuad(vertex, position, size))
                {
                    List<Vector3> ver = new List<Vector3>();
                    var leftBottomTileVertexWorldPosition = position - new Vector3(size / 2, size / 2);
                    var rightRopTileVertexWorldPosition = position + new Vector3(size / 2, size / 2);
                    var k = 0;
                    var buffer = new Dictionary<int, Vector3>();
                    
                    foreach (var vert in triangleByIndex.Value.Vertexes)
                    {
                        buffer.Add(vertices.Count + k, vert);
                        k++;
                    }
                    
                    foreach (var vert in buffer)
                    {
                        var roundedPosition = RoundVector(vert);
                        var indexes = buffer.Where(x => x.Key == vert.Key).Select(x => x.Key);

                        if (!_meshIndexAndLocalVertexIndexPairByPosition.ContainsKey(roundedPosition))
                        {
                            _meshIndexAndLocalVertexIndexPairByPosition.Add(roundedPosition, new Dictionary<int, List<int>>(){{meshIndex, indexes.ToList()} });
                        }
                        else
                        {
                            if (!_meshIndexAndLocalVertexIndexPairByPosition[roundedPosition].ContainsKey(meshIndex))
                            {
                                _meshIndexAndLocalVertexIndexPairByPosition[roundedPosition].Add(meshIndex, new List<int>());
                            }
                            _meshIndexAndLocalVertexIndexPairByPosition[roundedPosition][meshIndex].AddRange(indexes);
                        }
                        
                        if (_meshIndexAndLocalVertexIndexPairByGlobalVertexIndex.ContainsKey(triangleByIndex.Key))
                        {
                            _meshIndexAndLocalVertexIndexPairByGlobalVertexIndex[triangleByIndex.Key].TryAdd(meshIndex, new List<int>()
                                {vertices.Count, vertices.Count + 1, vertices.Count + 2});
                        }
                        else
                        {
                            var dict = new Dictionary<int, List<int>> { { meshIndex, new List<int>
                                {vertices.Count, vertices.Count + 1, vertices.Count + 2} } };
                            _meshIndexAndLocalVertexIndexPairByGlobalVertexIndex.Add(triangleByIndex.Key, dict);
                        }
                        
                        ver.Add(vert.Value);
                        colors.Add(Color.red);
                    }
                    
                    foreach (var v in ver)
                    {
                        var uvValue = new Vector2(GetUVValue(leftBottomTileVertexWorldPosition.x, rightRopTileVertexWorldPosition.x, v.x),
                            GetUVValue(leftBottomTileVertexWorldPosition.y, rightRopTileVertexWorldPosition.y, v.y));

                        uv.Add(uvValue);
                    }

                    for (int i = 0; i < ver.Count; i++)
                    {
                        ver[i] -= leftBottomTileVertexWorldPosition;
                    }
                    
                    vertices.AddRange(ver);
                    triangles.AddRange(GetTrianglesByVertex(ver.ToArray(), index, triangleByIndex.Value.IsRotated));
                    index++;
                    break;
                }
            }
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        go.transform.position = position - new Vector3(size / 2, size / 2);
        go.SetActive(true);

        return go;
    }

    private static Vector3 RoundVector(KeyValuePair<int, Vector3> vert)
    {
        return new Vector3((float)Math.Round(vert.Value.x, 2), (float)Math.Round(vert.Value.y, 2));
    }

    private float GetUVValue(float a, float b, float v)
    {
        var full = b - a;
        var fullValue = v - a;
        var one = full / 100;

        return fullValue / one / 100f;
    }

    private GameObject GlobalGenerate()
    {
        var mesh = new Mesh();
        _mesh = mesh;
        var go = Instantiate(_prefab);
        go.GetComponent<MeshFilter>().mesh = mesh;
        mesh.name = "Grid";
        var vertices = new List<Vector3>(_meshLength * _meshHeight * 3);
        var triangles = new List<int>(_meshLength * _meshHeight * 3);
        var uv = new List<Vector2>(_meshLength * _meshHeight * 3);

        for (int i = 0, index = 0; i < _meshHeight + 2; i++)
        {
            _offset.x = 0;
            if (_subRow >= 5)
                _subRow = 1;
            
            RefreshForTriangle(i, _height, _halfRadius, i + 1);
            
            for (int j = 0; j < _meshLength; j++, index++)
            {
                var newVertexes = OffsetTriangleByVector3(GetTriangleByRadius(_radius, IsNeedToRotate()), _offset);
                vertices.AddRange(newVertexes);
                var newTriangle = GetTrianglesByVertex(newVertexes, index, IsNeedToRotate());
                triangles.AddRange(newTriangle);
                _trianglesByIndex.Add(index, new Triangle(index, newVertexes, newTriangle, IsNeedToRotate()));
                
                _offset += new Vector3(_height * 2, 0);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        go.SetActive(false);
        return go;
    }

    private bool IsPointInQuad(Vector3 point, Vector3 quadCenter, float quadSize)
    {
        var halfSize = quadSize / 2;
        var a = new Vector3(quadCenter.x - halfSize, quadCenter.y + halfSize);
        var b = new Vector3(quadCenter.x + halfSize, quadCenter.y + halfSize);
        var c = new Vector3(quadCenter.x + halfSize, quadCenter.y - halfSize);
        var d = new Vector3(quadCenter.x - halfSize, quadCenter.y - halfSize);


        var inArea = Side(a, b, point) <= 0 
                     && Side(b, c, point) <= 0 
                     && Side(c, d, point) <= 0 
                     && Side(d, a, point) <= 0;

        return inArea;
        
        float Side(Vector3 a, Vector3 b, Vector3 p) => Mathf.Sign((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x));
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

    private void RefreshForTriangle(int i, float height, float halfRadius, int row)
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
    
    private class Triangle
    {
        public int TriangleIndex; 
        public Vector3[] Vertexes;
        public int[] VertexesIndex;
        public bool IsRotated;
        
        public Triangle(int triangleIndex, Vector3[] vertexes, int[] vertexesIndex, bool isRotated)
        {
            TriangleIndex = triangleIndex;
            Vertexes = vertexes;
            VertexesIndex = vertexesIndex;
            IsRotated = isRotated;
        }
    }
}