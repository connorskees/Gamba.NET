public class IndexWithMultitude
{
    public  IndexWithMultitude(int idx, int multitude=1)
    {
        this.idx = idx;
        this.multitude = multitude;
    }

    public string __str__()
    {
        return (((((((("[idx ") + (str(this.idx)))) + (", mult "))) + (str(this.multitude)))) + ("]"));
    }

}

public class Batch
{
    public  Batch(List<IndexWithMultitude> prevFactorIndices, List<IndexWithMultitude> factorIndices, HashSet<int> termIndices, List<(int, HashSet<IndexWithMultitude>)> nodesToTerms, List<List<IndexWithMultitude>> termsToNodes, List<bool> nodesTriviality, List<int> nodesOrder)
    {
        this.prevFactorIndices = prevFactorIndices;
        this.factorIndices = factorIndices;
        this.atoms = new() {  };
        this.children = new () {  };
        this.__partition(nodesToTerms, termIndices, termsToNodes, nodesTriviality, nodesOrder);
    }

