using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Communication;
using Communication.ActionPackage;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation.Samples.Communication;
using Button = UnityEngine.UIElements.Button;
using Color = UnityEngine.Color;
using Random = System.Random;

namespace Interaction.Drawable
{
    public class Drawable : ChangableColor
    {
        private bool _wasTouchedLastUpdate = false;
        [SerializeField] private GameObject lineObject;
        [SerializeField] private Image undoButton;
        private Vector2 _lastDrawPoint = Vector2.zero;
        private Line _currentLine;
        private readonly Dictionary<int, Line> _lines = new Dictionary<int, Line>();
        private Color _currentColor = Color.white;
        private int _currentWidth = 10;
        private readonly Random _idGenerator = new Random();
        private static bool _drawableIsEnabled = false;
        private static Drawable _drawable;
        private int _drawingId = 0;
        private int _screenWidth = 0, _screenHeight = 0;
        private UndoStack _linesProduced;
        private GraphicRaycaster _raycaster;

        void Awake(){
            _raycaster = FindObjectOfType<GraphicRaycaster>();
            _linesProduced = new UndoStack(undoButton);
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
        }

        public void SetDrawingID(int id)
        {
            _drawingId = id;
        }

        private void OnEnable()
        {
            _drawable = this;
            _drawableIsEnabled = true;
        }

        private void OnDisable()
        {
            ResetDrawing();
            _drawableIsEnabled = false;
        }

        public static bool IsDrawableAvailable()
        {
            return _drawableIsEnabled;
        }

        public static Drawable Get()
        {
            return _drawable;
        }

        private void FixedUpdate()
        {
            // Only every other frame should line-draw be noticed
            var isTouched = Input.touchCount != 0 && !IsClickOnUIElement();

            if (_wasTouchedLastUpdate)
            {
                if (!isTouched) FinishedDrawing();
                else ContinueDrawing();
            }
            else if (isTouched) StartDrawing();

            DequeueAndHandleSegmentPieces();
        }

        private void DequeueAndHandleSegmentPieces()
        {
            if (_unprocessedLinePieces.Count == 0) return;
            var linePiece = _unprocessedLinePieces.Dequeue();
        
            var newLineObject = SpawnDrawingObjects(ToScreenPixel(linePiece.A), ToScreenPixel(linePiece.B), linePiece.Width, linePiece.Color);
        
            linePiece.Line.AddToLine(newLineObject,linePiece.A,linePiece.B);
        }

        private Vector2 GetCurrentTouchPoint()
        {
            return Input.touches[0].position;
        } 

        private void StartDrawing()
        {
            _wasTouchedLastUpdate = true;
            _lastDrawPoint = GetCurrentTouchPoint();

            var id = _idGenerator.Next(Int32.MinValue, Int32.MaxValue);
            _currentLine = CreateLine(id, _currentWidth, _currentColor);
            _linesProduced.Push(id);
        }

        public void UndoLine()
        {
            var successfulPop = _linesProduced.TryPop(out var id);
            if (!successfulPop) return;
            RemoveLine(id);

            var removeAction = new RemoveLine(id);
            Stringifier.GetStringifier().StringifyAndForward<RemoveLine>(removeAction, TypeOfPackage.RemoveLine, true);
        }

        public void ResetAllDrawings()
        {
            if (_lines.Count == 0) return;
            
            ResetDrawing();
            var removeAction = new Empty();
            Stringifier.GetStringifier().StringifyAndForward<Empty>(removeAction, TypeOfPackage.RemoveDrawing, true);
        }

        private Line CreateLine(int id, int width, Color color)
        {
            var line = new Line(id, width, color, _drawingId);
            _lines.Add(line.id, line);
            return line;
        }

        private void ContinueDrawing()
        {
            var currentTouchPoint = GetCurrentTouchPoint();
            if (!ShouldDrawLine(_lastDrawPoint, currentTouchPoint)) return;

            var newLineObject = SpawnDrawingObjects(_lastDrawPoint, currentTouchPoint, _currentWidth, _currentColor);
            _currentLine.AddToLine(newLineObject, ToPointValue(_lastDrawPoint), ToPointValue(currentTouchPoint));
            _lastDrawPoint = currentTouchPoint;

            if (_currentLine.IsLineSegmentFinished())
            {
                DistributeSegment(_currentLine.GetCurrentLineSegment());
            }
        }

        private Vector2 FindMiddlePoint(Vector2 a, Vector2 b)
        {
            return new Vector2((a.x + b.x) / 2, (a.y + b.y) / 2);
        }

        private bool ShouldDrawLine(Vector2 from, Vector2 to)
        {
            return 2f < Vector2.Distance(from, to);
        }

        private GameObject SpawnDrawingObjects(Vector2 from, Vector2 to, int width, Color color)
        {
            // Get pos
            var position = FindMiddlePoint(from, to);

            // Get Angle
            var diff = to - from;
            var angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            var quaternion = Quaternion.AngleAxis(angle, Vector3.forward);
        
            // Create
            var newLineObject = Instantiate(lineObject, position, quaternion, transform);
            newLineObject.transform.Rotate(new Vector3(0,0,90));
        
            // Set scale
            var distance = Vector2.Distance(from, to);
            distance = Math.Min(distance, (10 + distance) * .6f);
            newLineObject.transform.localScale = new Vector3(width, distance, 1);
        
            // Set Color
            newLineObject.GetComponent<Image>().color = color;
        
            return newLineObject;
        }

        private void FinishedDrawing()
        {
            _wasTouchedLastUpdate = false;
            FinishLine();
        }

        private void FinishLine()
        {
            if (_currentLine.Segments.Count == 1 && _currentLine.Segments[0].length == 1)
            {
                _lines.Remove(_currentLine.id);
                _linesProduced.TryPop(out var x);
                return;
            }
            
            var segmentPiecesAreYetToBeDistributed = !_currentLine.IsLineSegmentFinished();
            if (segmentPiecesAreYetToBeDistributed)
            {
                DistributeSegment(_currentLine.GetCurrentLineSegment());
            }
        }
    
        public void QueueLineSegment(LineSegment segment)
        {
            if (segment.drawingId != _drawingId) return;

            var line = GetOrCreateLine(segment);

            var previousPoint = segment.points[0];

            for (var i = 1; i < LineSegment.MaxSize && i < segment.length; i++)
            {
                var newPoint = segment.points[i];
                _unprocessedLinePieces.Enqueue(new LineSegmentPiece(
                    previousPoint, newPoint, segment.width, line, segment.color));

                previousPoint = newPoint;
            }
        }

        public Vector2 ToPointValue(Vector2 input)
        {
            return new Vector2(input.x / _screenWidth, input.y / _screenHeight);
        }

        public Vector2 ToScreenPixel(Vector2 input)
        {
            return new Vector2(input.x * _screenWidth, input.y * _screenHeight);
        }

        public void SetCurrentColor(Color color)
        {
            _currentColor = color;
        }
    
        public void SetCurrentSize(int width)
        {
            _currentWidth = width;
        }

        private Queue<LineSegmentPiece> _unprocessedLinePieces = new Queue<LineSegmentPiece>();

        public void ResetDrawing()
        {
            var array = _lines.Keys.ToArray();
            foreach (var i in array)
            {
                RemoveLine(i);
            }
            
            _linesProduced.Clear();
            _lines.Clear();
        }

        private class UndoStack
        {
            private Stack<int> _stack;
            private Image _undoButtonRenderer;
            public readonly Color Unavailable = new Color(0.7f, 0.7f, 0.7f, .5f);
            public readonly Color Available = new Color(0.7f, 0.7f, 0.7f, 1);

            public UndoStack(Image undoButton)
            {
                _undoButtonRenderer = undoButton;
                SetButtonColor(Unavailable);
                _stack = new Stack<int>();
            }
            
            public void Clear()
            {
                if (_stack.Count != 0) SetButtonColor(Unavailable);
                _stack.Clear();
            }

            public void Push(int lineID)
            {
                _stack.Push(lineID);
                SetButtonColor(Available);
            }

            public bool TryPop(out int result)
            {
                var empty = _stack.TryPop(out result);
                if (_stack.Count == 0) SetButtonColor(Unavailable);
                return empty;
            }

            private void SetButtonColor(Color color)
            {
                _undoButtonRenderer.color = color;
            }
        }

        public void RemoveLine(int id)
        {
            if (!_lines.ContainsKey(id)) return;
            var line = _lines[id];
        
            foreach (var lineLineObject in line.LineObjects)
            {
                Destroy(lineLineObject);
            }

            if (line == _currentLine)
            {
                _wasTouchedLastUpdate = false;
            }

            _lines.Remove(id);
        }

        private void DistributeSegment(LineSegment segment)
        {
            Stringifier.GetStringifier().StringifyAndForward<LineSegment>(segment, TypeOfPackage.AddLineSegment, true);
        }

        public int GetID()
        {
            return _drawingId;
        }

        public LineSegment[] GetDrawing()
        {
            var size = _lines.Values.Aggregate(0, (acc, line) => acc + line.Segments.Count);
            var lineSegments = new LineSegment[size];

            var i = 0;
            foreach (var lineSegment in _lines.Values.SelectMany(line => line.Segments))
            {
                lineSegments[i] = lineSegment;
                i++;
            }

            return lineSegments;
        }

        private Line GetOrCreateLine(LineSegment segment)
        {
            if (!_lines.TryGetValue(segment.id, out var line)){
                line = CreateLine(segment.id, segment.width, segment.color);
            }

            return line;
        }

        public void ApplyLineSegment(LineSegment segment)
        {
            var line = GetOrCreateLine(segment);
        
            var previousPoint = segment.points[0];

            for (var i = 1; i < LineSegment.MaxSize && i < segment.length; i++)
            {
                var newPoint = segment.points[i];
            
                var newLineObject = SpawnDrawingObjects(ToScreenPixel(previousPoint),ToScreenPixel(newPoint), line.Width, line.Color);
                line.AddToLine(newLineObject,previousPoint,newPoint);
            
                previousPoint = newPoint;
            }
        }

        public void ApplyDrawing(LineSegment[] lineSegments)
        {
            ResetDrawing();
            var i = 0;
            for (; i < lineSegments.Length; i++)
            {
                ApplyLineSegment(lineSegments[i]);
            }
        }

        public override void ChangeColor(int colorIndex, BlockMaterials materials)
        {
            SetCurrentColor(materials.GetMaterial(colorIndex).color);
        }

        public bool IsClickOnUIElement()
        {
            //Set up the new Pointer Event
            var pointerEventData = new PointerEventData(EventSystem.current);
            //Set the Pointer Event Position to that of the mouse position
            pointerEventData.position = Input.mousePosition;

            //Create a list of Raycast Results
            List<RaycastResult> results = new List<RaycastResult>();

            //Raycast using the Graphics Raycaster and mouse click position
            _raycaster.Raycast(pointerEventData, results);
            
            return results.Any(result => result.gameObject.layer == 5);
        }
    }
}