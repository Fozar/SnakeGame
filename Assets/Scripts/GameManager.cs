using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.U2D;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;
    public int startPositionX;
    public int startPositionY;

    public GameObject mapPrefab;
    public GameObject playerHeadPrefab;
    public GameObject playerBodyPrefab;
    public GameObject mealPrefab;

    public SpriteAtlas snakeAtlas;

    private GameObject _player;
    private GameObject _mealObj;

    private Cell _mealCell;

    private Cell[,] _grid;
    private readonly List<Cell> _availableCells = new List<Cell>();
    private readonly List<SpecialCell> _segments = new List<SpecialCell>();

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    private bool _up, _down, _left, _right;

    public float moveRate = 0.5f;
    private float _timer;

    private Direction _currentDirection;

    #region Init

    private void Start()
    {
        CreateMap();
        PlacePlayer();
        SpawnMeal();
        _currentDirection = Direction.Up;
    }

    private void CreateMap()
    {
        var map = Instantiate(mapPrefab);
        var mapRenderer = map.GetComponent<SpriteRenderer>();
        mapRenderer.size = new Vector2(mapWidth, mapHeight);

        _grid = new Cell[mapWidth, mapHeight];
        for (var x = 0; x < mapWidth; x++)
        {
            for (var y = 0; y < mapHeight; y++)
            {
                var worldPosition = Vector3.zero;
                worldPosition.x = -(mapWidth / 2) + x + 0.5f;
                worldPosition.y = -(mapHeight / 2) + y + 0.5f;
                var cell = new Cell
                {
                    X = x,
                    Y = y,
                    WorldPosition = worldPosition
                };
                _grid[x, y] = cell;

                _availableCells.Add(cell);
            }
        }
    }

    private void PlacePlayer()
    {
        _player = new GameObject("Player");
        AddSegment(GetCell(startPositionX, startPositionY));
        AddSegment(GetCell(startPositionX, startPositionY - 1));
    }

    private void SpawnMeal()
    {
        _mealObj = Instantiate(mealPrefab);
        RandomlyPlaceMeal();
    }

    #endregion

    #region Update

    private void Update()
    {
        GetInput();
        SetPlayerDirection();

        _timer += Time.deltaTime;
        if (_timer <= moveRate) return;
        _timer = 0;
        MovePlayer();
    }

    private void GetInput()
    {
        _up = Input.GetButtonDown("Up");
        _down = Input.GetButtonDown("Down");
        _left = Input.GetButtonDown("Left");
        _right = Input.GetButtonDown("Right");
    }

    private void SetPlayerDirection()
    {
        if (_up && _currentDirection != Direction.Down)
        {
            _currentDirection = Direction.Up;
        }
        else if (_down && _currentDirection != Direction.Up)
        {
            _currentDirection = Direction.Down;
        }
        else if (_left && _currentDirection != Direction.Right)
        {
            _currentDirection = Direction.Left;
        }
        else if (_right && _currentDirection != Direction.Left)
        {
            _currentDirection = Direction.Right;
        }
    }

    private void MovePlayer()
    {
        var x = 0;
        var y = 0;
        switch (_currentDirection)
        {
            case Direction.Up:
                y = 1;
                break;
            case Direction.Down:
                y = -1;
                break;
            case Direction.Left:
                x = -1;
                break;
            case Direction.Right:
                x = 1;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var targetCell = GetCell(_segments[0].Cell.X + x, _segments[0].Cell.Y + y);
        if (targetCell == null || !_availableCells.Contains(targetCell))
        {
            print("Game over");
        }
        else
        {
            var isScore = targetCell == _mealCell;
            Cell previousCell = null;

            for (var i = 0; i < _segments.Count; i++)
            {
                var segment = _segments[i];

                if (i == 0)
                {
                    previousCell = segment.Cell;
                    segment.Cell = targetCell;

                    _availableCells.Remove(segment.Cell);
                    if (isScore)
                    {
                        if (_availableCells.Count > 0)
                        {
                            RandomlyPlaceMeal();
                        }
                        else
                        {
                            print("Win!");
                        }
                    }

                    DrawSegment(segment, previousCell);
                    _availableCells.Add(previousCell);
                }
                else
                {
                    var tempCell = segment.Cell;
                    var nextCell = _segments[i - 1].Cell;
                    if (i == _segments.Count - 1)
                    {
                        if (isScore)
                        {
                            var newSegment = AddSegment(previousCell);
                            DrawSegment(newSegment, tempCell, nextCell);
                            break;
                        }

                        _availableCells.Add(tempCell);
                        segment.Cell = previousCell;
                        _availableCells.Remove(segment.Cell);
                        DrawSegment(segment, nextCell: nextCell);
                    }
                    else
                    {
                        _availableCells.Add(tempCell);
                        segment.Cell = previousCell;
                        _availableCells.Remove(segment.Cell);
                        previousCell = tempCell;
                        DrawSegment(segment, previousCell, nextCell);
                    }
                }

                if (segment.Cell != null) segment.Obj.transform.position = segment.Cell.WorldPosition;
            }
        }
    }

    #endregion

    #region Utils

    private SpecialCell AddSegment(Cell cell)
    {
        GameObject prefab;
        switch (_segments.Count)
        {
            case 0:
                prefab = playerHeadPrefab;
                break;
            default:
                prefab = playerBodyPrefab;
                break;
        }

        var segment = Instantiate(prefab);
        var specialSegment = CreateSegmentCell(cell, segment);
        if (_segments.Count <= 1)
            _segments.Add(specialSegment);
        else
            _segments.Insert(_segments.Count - 1, specialSegment);
        _availableCells.Remove(cell);

        return specialSegment;
    }

    private void RandomlyPlaceMeal()
    {
        var rand = Random.Range(0, _availableCells.Count);
        var randomCell = _availableCells[rand];
        _mealObj.transform.position = randomCell.WorldPosition;
        _mealCell = randomCell;
    }

    private Cell GetCell(int x, int y)
    {
        return x < 0 || x > mapWidth - 1 || y < 0 || y > mapHeight - 1 ? null : _grid[x, y];
    }

    private SpecialCell CreateSegmentCell(Cell cell, GameObject segment)
    {
        var specialCell = new SpecialCell
        {
            Cell = cell,
            Obj = segment
        };
        specialCell.Obj.transform.parent = _player.transform;
        specialCell.Obj.transform.position = specialCell.Cell.WorldPosition;
        return specialCell;
    }

    private void DrawSegment(SpecialCell segment, [Optional] Cell previousCell, [Optional] Cell nextCell)
    {
        Sprite segmentSprite = null;
        if (previousCell != null && nextCell != null)
        {
            if (previousCell.Y < segment.Cell.Y && segment.Cell.Y < nextCell.Y ||
                previousCell.Y > segment.Cell.Y && segment.Cell.Y > nextCell.Y)
                segmentSprite = snakeAtlas.GetSprite("Snake_12");
            else if (previousCell.X < segment.Cell.X && segment.Cell.X < nextCell.X ||
                     previousCell.X > segment.Cell.X && segment.Cell.X > nextCell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_13");
            else if (previousCell.X < segment.Cell.X && segment.Cell.Y < nextCell.Y ||
                     previousCell.Y > segment.Cell.Y && segment.Cell.X > nextCell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_11");
            else if (previousCell.X > segment.Cell.X && segment.Cell.Y < nextCell.Y ||
                     previousCell.Y > segment.Cell.Y && segment.Cell.X < nextCell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_8");
            else if (previousCell.X > segment.Cell.X && segment.Cell.Y > nextCell.Y ||
                     previousCell.Y < segment.Cell.Y && segment.Cell.X < nextCell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_9");
            else if (previousCell.X < segment.Cell.X && segment.Cell.Y > nextCell.Y ||
                     previousCell.Y < segment.Cell.Y && segment.Cell.X > nextCell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_10");
        }
        else if (previousCell != null)
        {
            if (previousCell.Y < segment.Cell.Y)
                segmentSprite = snakeAtlas.GetSprite("Snake_0");
            else if (previousCell.Y > segment.Cell.Y)
                segmentSprite = snakeAtlas.GetSprite("Snake_2");
            else if (previousCell.X > segment.Cell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_3");
            else if (previousCell.X < segment.Cell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_1");
        }
        else if (nextCell != null)
        {
            if (nextCell.Y < segment.Cell.Y)
                segmentSprite = snakeAtlas.GetSprite("Snake_6");
            else if (nextCell.Y > segment.Cell.Y)
                segmentSprite = snakeAtlas.GetSprite("Snake_4");
            else if (nextCell.X > segment.Cell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_5");
            else if (nextCell.X < segment.Cell.X)
                segmentSprite = snakeAtlas.GetSprite("Snake_7");
        }

        segment.Obj.GetComponent<SpriteRenderer>().sprite = segmentSprite;
    }

    #endregion
}

internal class Cell
{
    public int X;
    public int Y;
    public Vector3 WorldPosition;
}

internal class SpecialCell
{
    public Cell Cell;
    public GameObject Obj;
}