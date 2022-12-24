using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Match3 : MonoBehaviour
{
    public ArrayLayout boardLayout;

    [Header("UI Elements")]
    public Sprite[] pieces;
    public RectTransform gameBoard;

    [Header("Prefabs")]
    public GameObject nodePiece;

    Node[,] board;
    int width;
    int height;
    int[] fills;

    List<NodePiece> updating;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;

    System.Random random;
    void Start()
    {
        width = 9;
        height = 14;
        StartGame();
    }

    void StartGame()
    {
        fills = new int[width];
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        updating = new List<NodePiece>();
        flipped = new List<FlippedPieces>();
        dead = new List<NodePiece>();

        InitializeBoard();
        VerifyBoard();
        InstantiateBoard();
    }

    private void Update()
    {
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for (int i = 0; i < updating.Count; i++)
        {
            NodePiece piece = updating[i];
            if (!piece.UpdatePiece()) finishedUpdating.Add(piece);
        }
        for (int i = 0; i < finishedUpdating.Count; i++)
        {
            NodePiece piece = finishedUpdating[i];
            FlippedPieces flip = getFlipped(piece);
            NodePiece flippedPiece = null;

            int x = (int)piece.index.x;
            fills[x] = Mathf.Clamp(fills[x] - 1, 0, width);

            List<Point> connected = isConnected(piece.index, true);

            bool wasFlipped = (flip != null);
            if (wasFlipped)
            { 
                flippedPiece = flip.getOtherPiece(piece);
                AddPoints(ref connected, isConnected(flippedPiece.index, true));
            }
            if (connected.Count  == 0)
            {
                if (wasFlipped)
                {
                    FlipPieces(piece.index, flippedPiece.index, false);
                }
            }
            else
            {
                foreach (Point point in connected)
                {
                    Node node = getNodeAtPoint(point);
                    NodePiece nodePiece = node.getPiece();
                    if (nodePiece != null)
                    {
                        nodePiece.gameObject.SetActive(false);
                        dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }
                ApplyGravityTo();
            }

            flipped.Remove(flip);
            updating.Remove(piece);
        }
    }

    void ApplyGravityTo()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = (height-1); y >= 0 ; y--)
            {
                Point p = new Point(x, y);
                Node node = getNodeAtPoint(p);
                int val = getValueAtPoint(p);
                if (val != 0) continue;
                for (int ny = (y-1); ny >= -1; ny--)
                {
                    Point next = new Point(x, ny);
                    int nextVal = getValueAtPoint(next);
                    if (nextVal == 0)
                        continue;
                    if(nextVal != -1)
                    {
                        Node set = getNodeAtPoint(next);
                        NodePiece piece = set.getPiece();

                        node.SetPiece(piece);
                        updating.Add(piece);

                        set.SetPiece(null);
                    }
                    else
                    {
                        int newVal = fillPiece();
                        NodePiece piece;
                        Point fallPoint = new Point(x, (-1 - fills[x]));
                        if(dead.Count > 0)
                        {
                            NodePiece revived = dead[0];
                            revived.gameObject.SetActive(true);
                            revived.rect.anchoredPosition = getPositionFromPoint(fallPoint);
                            piece = revived;

                            dead.RemoveAt(0);
                        }
                        else
                        {
                            GameObject newPiece = Instantiate(nodePiece, gameBoard);
                            NodePiece n = newPiece.GetComponent<NodePiece>();
                            RectTransform rect = newPiece.GetComponent<RectTransform>();
                            rect.anchoredPosition = getPositionFromPoint(fallPoint);
                            piece = n;
                        }
                        piece.Initialize(newVal, p, pieces[newVal - 1]);

                        Node hole = getNodeAtPoint(p);
                        hole.SetPiece(piece);
                        ResetPiece(piece);
                        fills[x]++;
                    }
                    break;
                }
            }
        }
    }

    FlippedPieces getFlipped(NodePiece p)
    {
        FlippedPieces flip = null;
        for (int i = 0; i < flipped.Count; i++)
        {
            if (flipped[i].getOtherPiece(p) != null)
            {
                flip = flipped[i];
                break;
            }
        }
        return flip;
    }
    void InitializeBoard()
    {
        board = new Node[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                board[x, y] = new Node(new Point(x, y), boardLayout.rows[y].row[x] ? -1 : fillPiece());
            }
        }
    }

    void VerifyBoard()
    {
        List<int> remove;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Point p = new Point(x, y);
                int val = getValueAtPoint(p);
                if (val <= 0) continue;

                remove = new List<int>();
                while (isConnected(p,true).Count > 0)
                {
                    val = getValueAtPoint(p);
                    if(remove.Contains(val))
                        remove.Add(val);
                    setValueAtPoint(p, newValue(ref remove));
                }
            }
        }
    }

    void InstantiateBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = getNodeAtPoint(new Point(x, y));
                int val = node.value;
                if (val <= 0) continue;
                    
                GameObject p = Instantiate(nodePiece, gameBoard);
                RectTransform rect = p.GetComponent<RectTransform>();
                NodePiece nodepiece = p.GetComponent<NodePiece>();
                rect.anchoredPosition = new Vector2(32 + (64 * x), -32 + (-64 * y));
                nodepiece.Initialize(val, new Point(x, y), pieces[val - 1]);
                node.SetPiece(nodepiece);
            }
        }
    }

    public void ResetPiece(NodePiece piece)
    {
        piece.ResetPosition();
        updating.Add(piece);
    }
    
    public void FlipPieces(Point one, Point two , bool main)
    {
        if (getValueAtPoint(one) < 0) return;
        Node nodeOne = getNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.getPiece();
        if(getValueAtPoint(two) > 0)
        {
            Node nodeTwo = getNodeAtPoint(two);
            NodePiece pieceTwo = nodeTwo.getPiece();
            nodeOne.SetPiece(pieceTwo);
            nodeTwo.SetPiece(pieceOne);

            if(main)
                flipped.Add(new FlippedPieces(pieceOne, pieceTwo));

            updating.Add(pieceOne);
            updating.Add(pieceTwo);
        }
        else
        {
            ResetPiece(pieceOne);
        }
    }
    public List<Point> isConnected(Point p, bool main)
    {
        List<Point> connected = new List<Point>();
        int val = getValueAtPoint(p);
        Point[] directions = { Point.up, Point.right, Point.down, Point.left };

        foreach (Point dir in directions)
        {
            List<Point> line = new List<Point>();

            int same = 0;
            for (int i = 1; i < 3; i++)
            {
                Point check = Point.add(p, Point.mult(dir, i));
                if (getValueAtPoint(check) == val)
                {
                    line.Add(check);
                    same++;
                }
            }

            if (same > 1)
                AddPoints(ref connected, line);
        }

        for (int i = 0; i < 2; i++)
        {
            List<Point> line = new List<Point>();
            int same = 0;
            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[i + 2]) };
            foreach (Point next in check)
            {
                if (getValueAtPoint(next) == val)
                {
                    line.Add(next);
                    same++;
                }
            }

            if (same > 1)
                AddPoints(ref connected, line);
        }

        for (int i = 0; i < 4; i++)
        {
            List<Point> square = new List<Point>();

            int same = 0;
            int next = i + 1;
            if (next >= 4)
                next -= 4;

            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[next]), Point.add(p,Point.add(directions[i], directions[next]))};
            foreach (Point pnt in check)
            {
                if (getValueAtPoint(pnt) == val)
                {
                    square.Add(pnt);
                    same++;
                }
            }

            if (same > 2)
                AddPoints(ref connected, square);
        }

        if (main)
        {
            for (int i = 0; i < connected.Count; i++)
                AddPoints(ref connected, isConnected(connected[i], false));
        }

        if (connected.Count > 0)
            connected.Add(p);

        return connected;
    }

    void AddPoints(ref List<Point> connected, List<Point> add)
    {
        foreach (Point p in add)
        {
            bool canAdd = true;
            for (int i = 0; i < connected.Count; i++)
            {
                if (connected[i].Equals(p))
                {
                    canAdd = false;
                    break;
                }
            }

            if (canAdd) connected.Add(p);
        }
    }

    int newValue(ref List<int> remove)
    {
        List<int> available = new List<int>();
        for (int i = 0; i < pieces.Length; i++)
            available.Add(i + 1);
        foreach (int i in remove)
            available.Remove(i);

        if (available.Count <= 0) return 0;

        return available[random.Next(0, available.Count)];

    }
    void setValueAtPoint(Point p, int v)
    {
        board[p.x, p.y].value = v;
    }
    int getValueAtPoint(Point p)
    {
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) return -1;
        return board[p.x, p.y].value;
    }
        
    int fillPiece()
    {
        int val = 0;
        val = (random.Next(0, 100) / (100 / pieces.Length) + 1);
        return val;
    }
    string getRandomSeed()
    {
        string seed = "";
        string acceptableChars = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm1234567890^#*";
        for (int i = 0; i < 20; i++)
        {
            seed += acceptableChars[Random.Range(0, acceptableChars.Length)];
        }
        return seed;
    }

    public Vector2 getPositionFromPoint(Point p)
    {
        return new Vector2(32 + (64 * p.x), -32 - (64 * p.y));
    }

    public Node getNodeAtPoint(Point p)
    {
        return board[p.x, p.y];
    }
}

[System.Serializable]
public class Node
{
    public Point index;
    public int value;

    NodePiece piece;
    public Node(Point p, int v)
    {
        index = p;
        value = v;
    }

    public void SetPiece(NodePiece p)
    {
        piece = p;
        value = (piece == null) ? 0 : piece.val;
        if (piece == null) return;
        piece.SetIndex(index);
    }

    public NodePiece getPiece()
    {
        return piece;
    }
}

[System.Serializable]
public class FlippedPieces
{
    NodePiece one;
    NodePiece two;

    public FlippedPieces(NodePiece o, NodePiece t)
    {
        one = o; two = t;
    }

    public NodePiece getOtherPiece(NodePiece p)
    {
        if (p == one)
            return two;
        else if (p == two)
            return one;
        else
            return null;
    }
}
