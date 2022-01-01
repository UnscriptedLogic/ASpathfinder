using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Node
{
    bool is_opened, is_closed;
    bool is_start, is_end, is_path;
    bool is_barrier;
    public int row, col;
    Vector2 total_rows;
    public Vector3 worldPos;
    public List<Node> neighbours = new List<Node>();
    public Renderer renderer;
    public Material pathMat;

    public float g_score;
    public float f_score;
    public Node came_from;

    public Node(int row, int col, Vector2 total_rows, Vector3 worldPos, Renderer renderer, Material pathMat)
    {
        this.row = row;
        this.col = col;
        this.total_rows = total_rows;
        this.worldPos = worldPos;
        this.renderer = renderer;
        this.pathMat = pathMat;
    }

    public bool isOpened() { return is_opened; }
    public bool isClosed() { return is_closed; }
    public bool isBarrier() { return is_barrier; }
    public bool isStart() { return is_start; }
    public bool isEnd() { return is_end; }
    public bool isPath() { return is_path; }

    public void set_opened() { is_opened = true; renderer.material.color = Color.green; }
    public void set_closed() { is_closed = true; renderer.material.color = Color.red; }
    public void set_Barrier() { is_barrier = true; renderer.material.color = Color.black; }
    public void set_start() { ResetNode(); is_start = true; renderer.material.color = Color.cyan; }
    public void set_end() { ResetNode(); is_end = true; renderer.material.color = Color.blue; }
    public void set_path() { is_path = true; renderer.material = pathMat; }
    public void ResetNode() { is_opened = false; is_closed = false; is_barrier = false; is_path = false; is_start = false; is_end = false; }
    public void UpdateNeighbours(Dictionary<Tuple<int, int>, Node> grid, int sub)
    {
        neighbours.Clear();

        Node value;

        List<Tuple<int, int>> positions = new List<Tuple<int, int>>();

        positions.Add(new Tuple<int, int>(row + sub, col));
        positions.Add(new Tuple<int, int>(row - sub, col));
        positions.Add(new Tuple<int, int>(row, col + sub));
        positions.Add(new Tuple<int, int>(row, col - sub));

        positions.Add(new Tuple<int, int>(row - sub, col - sub));
        positions.Add(new Tuple<int, int>(row - sub, col + sub));
        positions.Add(new Tuple<int, int>(row + sub, col - sub));
        positions.Add(new Tuple<int, int>(row + sub, col + sub));

        for (int i = 0; i < positions.Count; i++)
        {
            if (grid.TryGetValue(positions[i], out value) && !value.isBarrier())
            {
                neighbours.Add(value);
            }
        }
    }

    public void UpdateBarriers(Dictionary<Tuple<int, int>, Node> grid, float scansize, LayerMask layer)
    {
        if (Physics.CheckSphere(worldPos, scansize, layer))
        {
            set_Barrier();
            return;
        }

        ResetNode();
    }

}

public class PathfindingScript : MonoBehaviour
{
    [Header("Pathfinding Settings")]
    public Vector2 gridSize;

    [Range(1,15)]
    [Tooltip("How precise should each area be scanned. Careful when raising this above 10.")]
    public int subdivisions;
    int _subs = 1;
    [Tooltip("How large should each node scan for obstacles. This depicts how wide your character is and whether he can fit into tight spaces")]
    public float nodeScanSize;

    [Range(0.15f, 25f)]
    [Tooltip("[REQUIRES RELOAD] How often should the algorithm be executed? Careful when lowering this below 0.2")]
    public float refreshRate;

    [Tooltip("How far should the path be calculated. Raising this higher will affect performance")]
    public float calculationRange;

    [Tooltip("What layers should the agent consider obstacles")]
    public LayerMask layer;

    [Tooltip("The anchor of the plane the agent can walk on")]
    public Transform anchor;

    [Tooltip("Render the nodes. May slightly decrease frames based on the subdivisions")]
    public bool visualizeNodes;
    public bool visualizeGizmos;
    public bool visualizePath;
    public float gizmoSize;
    public Material nodeMat;

    public Transform destination;
    public Transform entity;

    //The actual grid
    public Dictionary<Tuple<int, int>, Node> grid = new Dictionary<Tuple<int, int>, Node>();
    List<Node> finalPath;
    List<Vector3> positions;

    Node startNode = null;
    Node endNode = null;

    public event Action<List<Vector3>> onPathRecalculated;

    public void DrawGrid()
    {
        grid.Clear();
        _subs = 11 - subdivisions;

        for (int i = 0; i < anchor.childCount; i++)
        {
            Destroy(anchor.GetChild(i).gameObject);
        }

        float closeStart = Mathf.Infinity;
        float closeEnd = Mathf.Infinity;
        Node _startNode = null;
        Node _endNode = null;

        for (int i = (int)-gridSize.x / 2; i < (int)gridSize.x / 2; i += _subs)
        {
            for (int j = (int)-gridSize.y / 2; j < (int)gridSize.y / 2; j += _subs)
            {
                //Creation of gameobjects
                GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(node.GetComponent<BoxCollider>());
                node.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Renderer nodeRenderer = node.GetComponent<Renderer>();
                nodeRenderer.enabled = false;
                nodeRenderer.material.color = Color.white;

                node.transform.SetParent(anchor);
                node.transform.position = anchor.position + new Vector3(i, anchor.position.y, j);
                node.transform.localScale = Vector3.one * nodeScanSize;

                //Creation of nodes and appending to the grid
                Node newNode = new Node(i, j, gridSize, node.transform.position, nodeRenderer, nodeMat);
                newNode.g_score = Mathf.Infinity;
                newNode.f_score = Mathf.Infinity;
                newNode.came_from = null;

                //Each node is tied to a specific position in the grid
                //To reference the correct node, co-ordinates have to be given
                Tuple<int, int> pos = new Tuple<int, int>(i, j);
                grid.Add(pos, newNode);

                //This sets the start and end nodes to the closest position to the actual destination
                float tmpStart = Vector3.Distance(newNode.worldPos, entity.position);
                if (tmpStart < closeStart)
                {
                    _startNode = newNode;
                    closeStart = tmpStart;
                    
                }

                float tmpEnd = Vector3.Distance(newNode.worldPos, destination.position);
                if (tmpEnd < closeEnd)
                {
                    _endNode = newNode;
                    closeEnd = tmpEnd;
                }

            }
        }

        startNode = _startNode;
        endNode = _endNode;

        startNode.set_start();
        endNode.set_end();
    }

    public void AStarAlgorithm()
    {
        NodeRefresh();

        //The open set of next possible nodes to examine
        List<Tuple<float, Node>> set = new List<Tuple<float, Node>>();

        //For performance, control keeps track of how many iterations to do to find the path
        //If a path does not exist, it will exit the while loop normally
        //If a path exists, while loop will be broken through ReconstructPath()
        int control = 0;
        while (control < grid.Count)
        {
            Node currentNode = null;

            //new node to examine will be the lowest f score from the existing batch
            float fscore = Mathf.Infinity;
            Node potentialCurrent = startNode;
            Tuple<float, Node> tmp = null;
            for (int i = 0; i < set.Count; i++)
            {
                if (set[i].Item1 < fscore)
                {
                    fscore = set[i].Item1;
                    potentialCurrent = set[i].Item2;
                    tmp = set[i];
                }
            }
            currentNode = potentialCurrent;

            //if the current node is the start node
            if (currentNode.isStart())
            {
                currentNode.g_score = 0;
                currentNode.f_score = Vector3.Distance(currentNode.worldPos, endNode.worldPos);
            }

            //if the current node is the end node
            if (currentNode.isEnd())
            {
                ReconstructPath(currentNode);
                break;
            }

            //remove that node from the open set
            set.Remove(tmp);

            //Get Neighbours of the node
            List<Node> neighbours = new List<Node>();
            for (int i = 0; i < currentNode.neighbours.Count; i++)
            {
                Tuple<int, int> pos = new Tuple<int, int>(currentNode.neighbours[i].row, currentNode.neighbours[i].col);
                neighbours.Add(grid[pos]);
            }

            //For every neighbour, check our g score to the neighbour's g-score
            for (int i = 0; i < neighbours.Count; i++)
            {
                float tmp_gscore = currentNode.g_score + 1;
                if (tmp_gscore < neighbours[i].g_score)
                {
                    //if it is lower, set it. set h scores and F score and came_from along side
                    neighbours[i].g_score = tmp_gscore;
                    neighbours[i].f_score = tmp_gscore + Vector3.Distance(neighbours[i].worldPos, endNode.worldPos);
                    neighbours[i].came_from = currentNode;

                    //if it is lower, add it the open set with its f score and set the nodes to open
                    Tuple<float, Node> opened_node = new Tuple<float, Node>(neighbours[i].f_score, neighbours[i]);
                    set.Add(opened_node);
                    neighbours[i].set_opened();
                }
            }

            control++;
            currentNode.set_closed();
        }
    }

    private void ReconstructPath(Node endNode)
    {
        finalPath = new List<Node>();
        finalPath.Clear();
        
        Node current = endNode;

        positions = new List<Vector3>();

        while (!current.isStart())
        {
            current.set_path();
            current = current.came_from;

            finalPath.Append(current);
            positions.Add(current.worldPos);
        }

        onPathRecalculated?.Invoke(positions);

        if (visualizePath)
        {
            Enumerable.Range(0, grid.Count).ToList().ForEach(p => {
                if (!grid.ElementAt(p).Value.isPath())
                {
                    grid.ElementAt(p).Value.renderer.enabled = false;
                } else
                {
                    grid.ElementAt(p).Value.renderer.enabled = true;
                }
            });
        }
    }

    public void NodeRefresh()
    {
        float closeStart = Mathf.Infinity;
        float closeEnd = Mathf.Infinity;
        Node _startNode = null;
        Node _endNode = null;
        startNode = null;
        endNode = null;

        Enumerable.Range(0, grid.Count).ToList().ForEach(p => {
            int row = grid.ElementAt(p).Value.row;
            int col = grid.ElementAt(p).Value.col;
            Tuple<int, int> pos = new Tuple<int, int>(row, col);
            grid.TryGetValue(pos, out Node value);

            value.ResetNode();
            value.UpdateNeighbours(grid, _subs);
            value.UpdateBarriers(grid, nodeScanSize, layer);
            value.g_score = Mathf.Infinity;
            value.f_score = Mathf.Infinity;
            value.came_from = null;

            float tmpStart = Vector3.Distance(value.worldPos, entity.position);
            if (tmpStart < closeStart)
            {
                _startNode = value;
                closeStart = tmpStart;

            }

            float tmpEnd = Vector3.Distance(value.worldPos, destination.position);
            if (tmpEnd < closeEnd)
            {
                _endNode = value;
                closeEnd = tmpEnd;
            }
        });

        startNode = _startNode;
        endNode = _endNode;

        startNode.set_start();
        endNode.set_end();
    }

    public List<Vector3> GetPath()
    {
        return positions;
    }

    private void Start()
    {
        DrawGrid();
        NodeRefresh();

        InvokeRepeating("AStarAlgorithm", 1f, refreshRate);
    }

    private void Update()
    {
        if (_subs != (11 - subdivisions))
        {
            DrawGrid();
        }
    }

    private void OnDrawGizmos()
    {
        if (visualizeGizmos)
        {
            for (int i = (int)-gridSize.x / 2; i < (int)gridSize.x / 2; i += (11 - subdivisions))
            {
                for (int j = (int)-gridSize.y / 2; j < (int)gridSize.y / 2; j += (11 - subdivisions))
                {
                    Gizmos.DrawSphere(anchor.position + new Vector3(i, anchor.position.y, j), gizmoSize);
                }
            }
        }
    }
}
