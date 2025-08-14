using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace GrozaGames.TopDownVision.MonoBehaviours
{
    public class VisionOrigin : MonoBehaviour
    {
        [Header("Vision")]
        [SerializeField] private string _visionLayer = "Vision";
        [SerializeField] private LayerMask _visionBlockers;
        [SerializeField] private Material _visionMaterial;
        
        [Header("Physics")] 
        [SerializeField] private float _originHeightOffset = 0.5f;
        [SerializeField] private float _raycastHitHeightOffset = 0.4f;
        [SerializeField] private float _raycastHitMaxDistance = 0.5f;
        [SerializeField] private float _raycastHitEpsilonPadding = 0.01f;
        
        [Header("Cone")]
        [SerializeField] private bool _coneEnabled = true;
        [SerializeField] private float _coneMaxDistance = 32f;
        [SerializeField] private float _coneAngle = 120f;
        [SerializeField] private int _coneRayCount = 360;
        [SerializeField] private float _coneEdgeFactor = 0.2f;
        
        [Header("Circle")]
        [SerializeField] private bool _circleEnabled = true;
        [SerializeField] private float _circleMaxDistance = 2f;
        [SerializeField] private int _circleRayCount = 120;

        private NativeArray<RaycastHit> _coneHits;
        private NativeArray<RaycastCommand> _coneCommands;
        private NativeArray<Segment> _coneSegments;
        private JobHandle _coneJobHandle;

        private NativeArray<RaycastHit> _circleHits;
        private NativeArray<RaycastCommand> _circleCommands;
        private NativeArray<Segment> _circleSegments;
        private JobHandle _circleJobHandle;
        
        private GameObject _coneGameObject;
        private MeshRenderer _coneMeshRenderer;
        private MeshFilter _coneMeshFilter;
        private Mesh _coneMesh;
        private NativeArray<Vector3> _coneVertices;
        private NativeArray<ushort> _coneTriangles;
        private NativeArray<Color> _coneColors;
        
        private GameObject _circleGameObject;
        private MeshRenderer _circleMeshRenderer;
        private MeshFilter _circleMeshFilter;
        private Mesh _circleMesh;
        private NativeArray<Vector3> _circleVertices;
        private NativeArray<ushort> _circleTriangles;
        private NativeArray<Color> _circleColors;

        private void Start()
        {
            _coneGameObject = new GameObject("Cone");
            _coneGameObject.transform.position = transform.position;
            _coneGameObject.transform.localRotation = transform.rotation;
            _coneGameObject.transform.localScale = Vector3.one;
            _coneGameObject.layer = LayerMask.NameToLayer(_visionLayer);
            _coneMeshRenderer = _coneGameObject.AddComponent<MeshRenderer>();
            _coneMeshRenderer.sharedMaterial = _visionMaterial;
            _coneMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _coneMeshFilter = _coneGameObject.AddComponent<MeshFilter>();
            _coneMesh = new Mesh();
            _coneMeshFilter.sharedMesh = _coneMesh;
            
            RecreateConeData();
            
            _circleGameObject = new GameObject("Circle");
            _circleGameObject.transform.position = transform.position;
            _circleGameObject.transform.localRotation = transform.rotation;
            _circleGameObject.transform.localScale = Vector3.one;
            _circleGameObject.layer = LayerMask.NameToLayer(_visionLayer);
            _circleMeshRenderer = _circleGameObject.AddComponent<MeshRenderer>();
            _circleMeshRenderer.sharedMaterial = _visionMaterial;
            _circleMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _circleMeshFilter = _circleGameObject.AddComponent<MeshFilter>();
            _circleMesh = new Mesh();
            _circleMeshFilter.sharedMesh = _circleMesh;

            RecreateCircleData();
        }

        private void OnDestroy()
        {
            _coneJobHandle.Complete();
            
            _coneHits.Dispose();
            _coneCommands.Dispose();
            _coneVertices.Dispose();
            _coneTriangles.Dispose();
            _coneSegments.Dispose();
            _coneColors.Dispose();
            Destroy(_coneMesh);
            
            _circleJobHandle.Complete();
            
            _circleHits.Dispose();
            _circleCommands.Dispose();
            _circleVertices.Dispose();
            _circleTriangles.Dispose();
            _circleSegments.Dispose();
            _circleColors.Dispose();
            Destroy(_circleMesh);
        }

        public void Enable()
        {
            enabled = true;
            if (_coneEnabled && _coneGameObject)
                _coneGameObject.SetActive(true);
            if (_circleEnabled && _circleGameObject)
                _circleGameObject.SetActive(true);
        }

        public void Disable()
        {
            enabled = false;
            if (_coneEnabled && _coneGameObject)
                _coneGameObject.SetActive(false);
            if (_circleEnabled && _circleGameObject)
                _circleGameObject.SetActive(false);
        }

        private void Update()
        {
            UpdateCone();
            UpdateCircle();
        }

        private void UpdateCone()
        {
            if (!_coneEnabled)
            {
                _coneMesh.Clear();
                _coneGameObject.SetActive(false);
                return;
            }
            
            _coneGameObject.SetActive(true);
            
            
            
            if (_coneRayCount * 2 != _coneHits.Length)
                RecreateConeData();

            
            
            for (var i = 0; i < _coneRayCount; i++)
            {
                var angle = _coneAngle * ((float) i / _coneRayCount - 0.5f);
                var direction = Quaternion.Euler(0, angle, 0) * transform.forward;
                var source = transform.position + Vector3.up * _originHeightOffset;
                _coneCommands[i] = new RaycastCommand(source, direction, new QueryParameters(_visionBlockers), _coneMaxDistance);
                _coneCommands[i + _coneRayCount] = new RaycastCommand(source + Vector3.up * _raycastHitHeightOffset, direction, new QueryParameters(_visionBlockers), _coneMaxDistance);
            }
            
            _coneJobHandle = RaycastCommand.ScheduleBatch(_coneCommands, _coneHits, 1);
            _coneJobHandle.Complete();
            
            
            
            var coneEdgeMaxCount = (int) (_coneRayCount * _coneEdgeFactor * 0.5f);
            var totalVertices = 0;
            for (var i = 0; i < _coneRayCount; i++)
            {
                var currentHitIndex = i;
                var nextHitIndex = (i + 1) % _coneRayCount;
                
                var edgeFactor0 = currentHitIndex < coneEdgeMaxCount 
                    ? (float) currentHitIndex / coneEdgeMaxCount 
                    : currentHitIndex > _coneRayCount - coneEdgeMaxCount ? 1f - (float) (currentHitIndex - (_coneRayCount - coneEdgeMaxCount)) / coneEdgeMaxCount : 1f;
                
                var edgeFactor1 = nextHitIndex < coneEdgeMaxCount 
                    ? (float) nextHitIndex / coneEdgeMaxCount 
                    : nextHitIndex > _coneRayCount - coneEdgeMaxCount ? 1f - (float) (nextHitIndex - (_coneRayCount - coneEdgeMaxCount)) / coneEdgeMaxCount : 1f;

                var segment = new Segment();
                
                segment.Origin = Vector3.up * _originHeightOffset;
                segment.OriginColor = GetColor(Mathf.Min(edgeFactor0, edgeFactor1));
                segment.OriginIndex = totalVertices;
                totalVertices++;
                
                var hit0 = _coneHits[i];
                var hasHit0 = hit0.collider != null;
                
                var pointA0 = hasHit0
                    ? hit0.point
                    : transform.position + Vector3.up * _originHeightOffset + _coneCommands[currentHitIndex].direction * _coneMaxDistance;
                segment.PointA0 = pointA0 - transform.position - _raycastHitEpsilonPadding * _coneCommands[currentHitIndex].direction;
                segment.PointA0Color = GetColor(edgeFactor0 * (1f - Vector3.Distance(transform.position, WithY(pointA0,transform.position.y)) / _coneMaxDistance));
                segment.PointA0Index = totalVertices;
                totalVertices++;
                
                var hit1 = _coneHits[nextHitIndex];
                var hasHit1 = hit1.collider != null;
                
                var pointB0 = hasHit1
                    ? hit1.point
                    : transform.position + Vector3.up * _originHeightOffset + _coneCommands[nextHitIndex].direction * _coneMaxDistance;
                segment.PointB0 = pointB0 - transform.position - _raycastHitEpsilonPadding * _coneCommands[nextHitIndex].direction;
                segment.PointB0Color = GetColor(edgeFactor1 * (1f - Vector3.Distance(transform.position, WithY(pointB0,transform.position.y)) / _coneMaxDistance));
                segment.PointB0Index = totalVertices;
                totalVertices++;

                if (hasHit0 && hasHit1)
                {
                    var currentAdditionalHitIndex = _coneRayCount + currentHitIndex;
                    var nextAdditionalHitIndex = _coneRayCount + nextHitIndex;
                    
                    var additionalHit0 = _coneHits[currentAdditionalHitIndex];
                    var additionalHit1 = _coneHits[nextAdditionalHitIndex];
                    
                    var hasAdditionalHit0 = additionalHit0.collider != null;
                    var hasAdditionalHit1 = additionalHit1.collider != null;
                    
                    if (hasAdditionalHit0 && hasAdditionalHit1
                                          && Mathf.Abs(Vector3.SqrMagnitude(additionalHit0.point - segment.Origin) - Vector3.SqrMagnitude(additionalHit1.point - segment.Origin)) < _raycastHitMaxDistance * _raycastHitMaxDistance
                                          && Vector3.SqrMagnitude(additionalHit0.point - hit0.point) < _raycastHitMaxDistance * _raycastHitMaxDistance
                                          && Vector3.SqrMagnitude(additionalHit1.point - hit1.point) < _raycastHitMaxDistance * _raycastHitMaxDistance)
                    {
                        segment.HasAdditionalPoints = true;
                        
                        var pointA1 = additionalHit0.point;
                        var pointB1 = additionalHit1.point;
                        
                        segment.PointA1 = pointA1 - transform.position - _raycastHitEpsilonPadding * _coneCommands[currentAdditionalHitIndex].direction;
                        segment.PointA1Color = segment.PointA0Color;
                        segment.PointA1Index = totalVertices;
                        totalVertices++;
                        
                        segment.PointB1 = pointB1 - transform.position - _raycastHitEpsilonPadding * _coneCommands[nextAdditionalHitIndex].direction;
                        segment.PointB1Color = segment.PointB0Color;
                        segment.PointB1Index = totalVertices;
                        totalVertices++;
                    }
                }
                
                _coneSegments[i] = segment;
            }

            for (int i = 0; i < _coneVertices.Length; i++)
            {
                _coneVertices[i] = Vector3.zero;
                _coneColors[i] = GetColor(0);
            }
            
            var triangleIndex = 0;
            for (var i = 0; i < _coneSegments.Length; i++)
            {
                var segment = _coneSegments[i];
                
                // Vertices
                
                _coneVertices[segment.OriginIndex] = segment.Origin;
                _coneVertices[segment.PointA0Index] = segment.PointA0;
                _coneVertices[segment.PointB0Index] = segment.PointB0;
                
                _coneColors[segment.OriginIndex] = segment.OriginColor;
                _coneColors[segment.PointA0Index] = segment.PointA0Color;
                _coneColors[segment.PointB0Index] = segment.PointB0Color;

                if (segment.HasAdditionalPoints)
                {
                    _coneVertices[segment.PointA1Index] = segment.PointA1;
                    _coneVertices[segment.PointB1Index] = segment.PointB1;

                    _coneColors[segment.PointA1Index] = segment.PointA1Color;
                    _coneColors[segment.PointB1Index] = segment.PointB1Color;
                }
                
                // Triangles

                _coneTriangles[triangleIndex + 0] = (ushort)segment.OriginIndex;
                _coneTriangles[triangleIndex + 1] = (ushort)segment.PointA0Index;
                _coneTriangles[triangleIndex + 2] = (ushort)segment.PointB0Index;
                triangleIndex += 3;

                if (segment.HasAdditionalPoints)
                {
                    _coneTriangles[triangleIndex + 0] = (ushort)segment.PointA0Index;
                    _coneTriangles[triangleIndex + 1] = (ushort)segment.PointA1Index;
                    _coneTriangles[triangleIndex + 2] = (ushort)segment.PointB0Index;

                    _coneTriangles[triangleIndex + 3] = (ushort)segment.PointA1Index;
                    _coneTriangles[triangleIndex + 4] = (ushort)segment.PointB1Index;
                    _coneTriangles[triangleIndex + 5] = (ushort)segment.PointB0Index;

                    triangleIndex += 6;
                }
            }
            
            _coneMesh.Clear();
            _coneMesh.SetVertices(_coneVertices);
            _coneMesh.SetIndices(_coneTriangles, MeshTopology.Triangles, 0);
            _coneMesh.SetColors(_coneColors);
            
            _coneMeshFilter.sharedMesh = _coneMesh;
            _coneGameObject.transform.position = transform.position;
        }

        private void UpdateCircle()
        {
            if (!_circleEnabled)
            {
                _circleMesh.Clear();
                _circleGameObject.SetActive(false);
                return;
            }

            _circleGameObject.SetActive(true);

            if (_circleRayCount * 2 != _circleHits.Length)
                RecreateCircleData();

            for (var i = 0; i < _circleRayCount; i++)
            {
                var direction = Quaternion.Euler(0, 360 * ((float) i / (_circleRayCount - 1)), 0) * Vector3.forward;
                var source = transform.position + Vector3.up * _originHeightOffset;
                _circleCommands[i] = new RaycastCommand(source, direction, new QueryParameters(_visionBlockers), _circleMaxDistance);
                _circleCommands[i + _circleRayCount] = new RaycastCommand(source + Vector3.up * _raycastHitHeightOffset, direction, new QueryParameters(_visionBlockers), _circleMaxDistance);
            }
            
            _circleJobHandle = RaycastCommand.ScheduleBatch(_circleCommands, _circleHits, 1);
            _circleJobHandle.Complete();
            
            var totalVertices = 0;
            for (var i = 0; i < _circleRayCount; i++)
            {
                var currentHitIndex = i;
                var nextHitIndex = (i + 1) % _circleRayCount;
                
                var segment = new Segment();
                
                segment.Origin = Vector3.up * _originHeightOffset;
                segment.OriginColor = GetColor(1);
                segment.OriginIndex = totalVertices;
                totalVertices++;
                
                var hit0 = _circleHits[i];
                var hasHit0 = hit0.collider != null;
                
                var pointA0 = hasHit0
                    ? hit0.point
                    : transform.position + Vector3.up * _originHeightOffset + _circleCommands[currentHitIndex].direction * _circleMaxDistance;
                segment.PointA0 = pointA0 - transform.position - _raycastHitEpsilonPadding * _circleCommands[currentHitIndex].direction;
                segment.PointA0Color = GetColor(1f - Vector3.Distance(transform.position, WithY(pointA0,transform.position.y)) / _circleMaxDistance);
                segment.PointA0Index = totalVertices;
                totalVertices++;
                
                var hit1 = _circleHits[nextHitIndex];
                var hasHit1 = hit1.collider != null;
                
                var pointB0 = hasHit1
                    ? hit1.point
                    : transform.position + Vector3.up * _originHeightOffset + _circleCommands[nextHitIndex].direction * _circleMaxDistance;
                segment.PointB0 = pointB0 - transform.position - _raycastHitEpsilonPadding * _circleCommands[nextHitIndex].direction;
                segment.PointB0Color = GetColor(1f - Vector3.Distance(transform.position, WithY(pointB0,transform.position.y)) / _circleMaxDistance);
                segment.PointB0Index = totalVertices;
                totalVertices++;

                if (hasHit0 && hasHit1)
                {
                    var currentAdditionalHitIndex = _circleRayCount + currentHitIndex;
                    var nextAdditionalHitIndex = _circleRayCount + nextHitIndex;
                    
                    var additionalHit0 = _circleHits[currentAdditionalHitIndex];
                    var additionalHit1 = _circleHits[nextAdditionalHitIndex];
                    
                    var hasAdditionalHit0 = additionalHit0.collider != null;
                    var hasAdditionalHit1 = additionalHit1.collider != null;
                    
                    if (hasAdditionalHit0 && hasAdditionalHit1
                                          && Mathf.Abs(Vector3.SqrMagnitude(additionalHit0.point - segment.Origin) - Vector3.SqrMagnitude(additionalHit1.point - segment.Origin)) < _raycastHitMaxDistance * _raycastHitMaxDistance
                                          && Vector3.SqrMagnitude(additionalHit0.point - hit0.point) < _raycastHitMaxDistance * _raycastHitMaxDistance
                                          && Vector3.SqrMagnitude(additionalHit1.point - hit1.point) < _raycastHitMaxDistance * _raycastHitMaxDistance)
                    {
                        segment.HasAdditionalPoints = true;
                        
                        var pointA1 = additionalHit0.point;
                        var pointB1 = additionalHit1.point;
                        
                        segment.PointA1 = pointA1 - transform.position - _raycastHitEpsilonPadding * _circleCommands[currentAdditionalHitIndex].direction;
                        segment.PointA1Color = segment.PointA0Color;
                        segment.PointA1Index = totalVertices;
                        totalVertices++;
                        
                        segment.PointB1 = pointB1 - transform.position - _raycastHitEpsilonPadding * _circleCommands[nextAdditionalHitIndex].direction;
                        segment.PointB1Color = segment.PointB0Color;
                        segment.PointB1Index = totalVertices;
                        totalVertices++;
                    }
                }
                
                _circleSegments[i] = segment;
            }

            for (int i = 0; i < _circleVertices.Length; i++)
            {
                _circleVertices[i] = Vector3.zero;
                _circleColors[i] = GetColor(0);
            }
            
            var triangleIndex = 0;
            for (var i = 0; i < _circleSegments.Length; i++)
            {
                var segment = _circleSegments[i];
                
                // Vertices
                
                _circleVertices[segment.OriginIndex] = segment.Origin;
                _circleVertices[segment.PointA0Index] = segment.PointA0;
                _circleVertices[segment.PointB0Index] = segment.PointB0;
                
                _circleColors[segment.OriginIndex] = segment.OriginColor;
                _circleColors[segment.PointA0Index] = segment.PointA0Color;
                _circleColors[segment.PointB0Index] = segment.PointB0Color;

                if (segment.HasAdditionalPoints)
                {
                    _circleVertices[segment.PointA1Index] = segment.PointA1;
                    _circleVertices[segment.PointB1Index] = segment.PointB1;

                    _circleColors[segment.PointA1Index] = segment.PointA1Color;
                    _circleColors[segment.PointB1Index] = segment.PointB1Color;
                }
                
                // Triangles

                _circleTriangles[triangleIndex + 0] = (ushort)segment.OriginIndex;
                _circleTriangles[triangleIndex + 1] = (ushort)segment.PointA0Index;
                _circleTriangles[triangleIndex + 2] = (ushort)segment.PointB0Index;
                triangleIndex += 3;

                if (segment.HasAdditionalPoints)
                {
                    _circleTriangles[triangleIndex + 0] = (ushort)segment.PointA0Index;
                    _circleTriangles[triangleIndex + 1] = (ushort)segment.PointA1Index;
                    _circleTriangles[triangleIndex + 2] = (ushort)segment.PointB0Index;

                    _circleTriangles[triangleIndex + 3] = (ushort)segment.PointA1Index;
                    _circleTriangles[triangleIndex + 4] = (ushort)segment.PointB1Index;
                    _circleTriangles[triangleIndex + 5] = (ushort)segment.PointB0Index;

                    triangleIndex += 6;
                }
            }
            
            _circleMesh.Clear();
            _circleMesh.SetVertices(_circleVertices);
            _circleMesh.SetIndices(_circleTriangles, MeshTopology.Triangles, 0);
            _circleMesh.SetColors(_circleColors);
            
            _circleMeshFilter.sharedMesh = _circleMesh;
            _circleGameObject.transform.position = transform.position;
        }

        private Vector3 WithY(Vector3 point, float y)
        {
            return new Vector3(point.x, y, point.z);
        }

        private void RecreateConeData()
        {
            if (_coneHits.IsCreated)
                _coneHits.Dispose();

            if (_coneCommands.IsCreated)
                _coneCommands.Dispose();

            _coneHits = new NativeArray<RaycastHit>(_coneRayCount * 2, Allocator.Persistent);
            _coneCommands = new NativeArray<RaycastCommand>(_coneRayCount * 2, Allocator.Persistent);
            
            if (_coneVertices.IsCreated)
                _coneVertices.Dispose();

            if (_coneTriangles.IsCreated)
                _coneTriangles.Dispose();
            
            if (_coneColors.IsCreated)
                _coneColors.Dispose();
            
            if (_coneSegments.IsCreated)
                _coneSegments.Dispose();
            
            _coneVertices = new NativeArray<Vector3>(_coneRayCount * 5, Allocator.Persistent);
            _coneTriangles = new NativeArray<ushort>(_coneRayCount * 3 * 3, Allocator.Persistent);
            _coneColors = new NativeArray<Color>(_coneRayCount * 5, Allocator.Persistent);
            _coneSegments = new NativeArray<Segment>(_coneRayCount, Allocator.Persistent);
        }

        private void RecreateCircleData()
        {
            if (_circleHits.IsCreated)
                _circleHits.Dispose();

            if (_circleCommands.IsCreated)
                _circleCommands.Dispose();

            _circleHits = new NativeArray<RaycastHit>(_circleRayCount * 2, Allocator.Persistent);
            _circleCommands = new NativeArray<RaycastCommand>(_circleRayCount * 2, Allocator.Persistent);
            
            if (_circleVertices.IsCreated)
                _circleVertices.Dispose();

            if (_circleTriangles.IsCreated)
                _circleTriangles.Dispose();

            if (_circleColors.IsCreated)
                _circleColors.Dispose();

            if (_circleSegments.IsCreated)
                _circleSegments.Dispose();
            
            _circleVertices = new NativeArray<Vector3>(_circleRayCount * 5, Allocator.Persistent);
            _circleTriangles = new NativeArray<ushort>(_circleRayCount * 3 * 3, Allocator.Persistent);
            _circleColors = new NativeArray<Color>(_circleRayCount * 5, Allocator.Persistent);
            _circleSegments = new NativeArray<Segment>(_circleRayCount, Allocator.Persistent);
        }

        private Color GetColor(float t)
        {
            const float value = 1f;
            return new Color(value, value, value, t);
        }
        
        private struct Segment
        {
            public Vector3 Origin;
            public int OriginIndex;
            public Color OriginColor;
            
            public Vector3 PointA0;
            public int PointA0Index;
            public Color PointA0Color;
            
            public Vector3 PointB0;
            public int PointB0Index;
            public Color PointB0Color;

            public bool HasAdditionalPoints;
            
            public Vector3 PointA1;
            public int PointA1Index;
            public Color PointA1Color;
            
            public Vector3 PointB1;
            public int PointB1Index;
            public Color PointB1Color;
        }
    }
}
