public class Node
{
    public  Node(NodeType nodeType, long modulus, bool modRed=false)
    {
        this.type = nodeType;
        this.children = new List<dynamic>() {  };
        this.vname = "";
        this.__vidx = -(1);
        this.constant = 0;
        this.state = NodeState.UNKNOWN;
        this.__modulus = modulus;
        this.__modRed = modRed;
        this.linearEnd = 0;
        this.__MAX_IT = 10;
    }

    public string __str__()
    {
        return this.to_string();
    }

