using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gamba.Prototyping.Transpiled
{
    public class Batch
    {
        public readonly List<IndexWithMultitude> prevFactorIndices;

        public readonly List<IndexWithMultitude> factorIndices;

        private readonly HashSet<int> termIndices;

        private readonly List<(int, HashSet<IndexWithMultitude>)> nodesToTerms;

        private readonly List<List<IndexWithMultitude>> termsToNodes;

        private readonly List<bool> nodesTriviality;

        private readonly List<int> nodesOrder;

        public HashSet<int> atoms;

        public List<Batch> children;

        public Batch(List<IndexWithMultitude> prevFactorIndices, List<IndexWithMultitude> factorIndices, HashSet<int> termIndices, List<(int, HashSet<IndexWithMultitude>)> nodesToTerms, List<HashSet<IndexWithMultitude>> termsToNodes, List<bool> nodesTriviality, List<int> nodesOrder)
        {
            this.prevFactorIndices = prevFactorIndices;
            this.factorIndices = factorIndices;

            this.prevFactorIndices = prevFactorIndices;
            this.factorIndices = factorIndices;
            this.atoms = new() { };
            this.children = new() { };
            this.__partition(nodesToTerms, termIndices, termsToNodes, nodesTriviality, nodesOrder);
        }

        public void __partition(List<(int, HashSet<IndexWithMultitude>)> nodesToTerms, HashSet<int> termIndices, List<HashSet<IndexWithMultitude>> termsToNodes, List<bool> nodesTriviality, List<int> nodesOrder)
        {
            //var todo = new() { termIndices };
            var todo = termIndices.ToList().ToHashSet();

            while (true)
            {
                if ((nodesToTerms.Count()) == (0))
                {
                    break;
                }
                var idx = this.__get_next_batch(nodesToTerms, termsToNodes, nodesTriviality, nodesOrder);
                if ((idx) == (null))
                {
                    break;
                }
                Assert.True((nodesToTerms[idx].Item2.Count()) > (1));
                var factor = nodesToTerms[idx].Item1;
                var multitude = this.__get_lowest_multitude(nodesToTerms[idx].Item2);
                List<IndexWithMultitude> factors = new() { new IndexWithMultitude(factor, multitude) };
                HashSet<int> terms = new HashSet<int>(nodesToTerms[idx].Item2.Where(p => true).Select(p => p.idx).ToList());
                List<(int, HashSet<IndexWithMultitude>)> ntt = new() { };
                nodesToTerms[idx] = (nodesToTerms[idx].Item1, this.__reduce_multitudes(nodesToTerms[idx].Item2, multitude));
                if ((nodesToTerms[idx].Item2.Count()) > (0))
                {
                    ntt.Add(nodesToTerms[idx]);
                }
                nodesToTerms.RemoveAt(idx);
                foreach (var i in Range.Get(((nodesToTerms.Count()) - (1)), -(1), -(1)))
                {
                    var b = nodesToTerms[i];
                    var t = b.Item2.Where(p => true).Select(p => p.idx).ToHashSet();
                    var inters = t.ToHashSet();
                    inters.IntersectWith(terms);
                        
                    if ((inters.Count()) > (1))
                    {
                        if ((inters.Count()) == (terms.Count()))
                        {
                            var spl = b.Item2.Where(p => ((inters).Contains(p.idx))).Select(p => p).ToHashSet();
                            multitude = this.__get_lowest_multitude(spl);
                            factors.Add(new IndexWithMultitude(b.Item1, multitude));
                            spl = this.__reduce_multitudes(spl, multitude);
                            if ((spl.Count()) > (0))
                            {
                                ntt.Add(( b.Item1, spl ));
                            }
                        }
                        else
                        {
                            ntt.Add(( b.Item1, b.Item2.Where(p => ((inters).Contains(p.idx))).Select(p => p).ToHashSet() ));
                        }
                        b.Item2 = b.Item2.Where(p => !((inters).Contains(p.idx))).Select(p => p).ToHashSet();
                        if ((b.Item2.Count()) <= (1))
                        {
                            nodesToTerms.RemoveAt(i);
                        }
                    }
                    else
                    {
                        if ((inters.Count()) > (0))
                        {
                            b.Item2 = b.Item2.Where(p => !((inters).Contains(p.idx))).Select(p => p).ToHashSet();
                            if ((b.Item2.Count()) <= (1))
                            {
                                nodesToTerms.RemoveAt(i);
                            }
                        }
                    }
                }
                this.children.Add(new Batch((factorIndices.Concat(this.prevFactorIndices)).ToList(), factors, terms, ntt, termsToNodes, nodesTriviality, nodesOrder));
                todo.RemoveWhere(x => terms.Contains(x));
                //todo -= terms;
            }
            this.atoms = todo;
        }

        public NullableI32 __get_next_batch(List<(int, HashSet<IndexWithMultitude>)> nodesToTerms, List<HashSet<IndexWithMultitude>> termsToNodes, List<bool> nodesTriviality, List<int> nodesOrder)
        {
            var indices = this.__get_largest_termset_indices(nodesToTerms, termsToNodes, nodesTriviality);
            if ((indices) == (null))
            {
                indices = this.__get_largest_termset_indices(nodesToTerms);
            }
            if ((indices) == (null))
            {
                return null;
            }
            if ((indices.Count()) == (1))
            {
                return indices[0];
            }
            var collected = this.__collect_largest_batches(nodesToTerms, indices);
            var largest = this.__get_largest_list_indices(collected);
            foreach (var o in nodesOrder)
            {
                if (((largest).Contains(o)))
                {
                    return indices[o];
                }
            }
            Assert.True(false);
            return indices[largest[0]];
        }

        public List<List<int>> __collect_largest_batches(List<(int, HashSet<IndexWithMultitude>)> nodesToTerms, List<int> indices)
        {
            List<List<int>> collected = new() { };
            var i = 0;
            while (true)
            {
                if ((i) == (indices.Count()))
                {
                    break;
                }
                var found = false;
                foreach (var j in Range.Get(i))
                {
                    if ((nodesToTerms[i]) == (nodesToTerms[j]))
                    {
                        collected[j].Add(i);
                        indices.RemoveAt(i);
                        found = true;
                        break;
                    }
                }
                if (!(found))
                {
                    collected.Add(new() { i });
                    i += 1;
                }
            }
            return collected;
        }

        public long __get_lowest_multitude(HashSet<IndexWithMultitude> indicesWithMultitude)
        {
            return indicesWithMultitude.MinBy(x => x.multitude).multitude;
            //return Math.Min(indicesWithMultitude).multitude;
        }

        public HashSet<IndexWithMultitude> __reduce_multitudes(HashSet<IndexWithMultitude> indicesWithMultitude, long delta)
        {
            foreach (var p in indicesWithMultitude)
            {
                Assert.True((p.multitude) >= (delta));
                p.multitude -= delta;
            }
            return indicesWithMultitude.Where(p => (p.multitude) > (0)).Select(p => p).ToHashSet();
        }

        public List<int> __get_largest_termset_indices(List<(int, HashSet<IndexWithMultitude>)> pairs, List<HashSet<IndexWithMultitude>> termsToNodes = null, List<bool> nodesTriviality = null)
        {
            Assert.True((pairs.Count()) > (0));
            Assert.True(((termsToNodes) == (null)) == ((nodesTriviality) == (null)));
            List<int> indices = null;
            NullableI32 maxl = null;
            foreach (var i in Range.Get(pairs.Count()))
            {
                var l = pairs[i].Item2.Count();
                if ((l) < (2))
                {
                    continue;
                }
                if ((((maxl) != (null)) && ((l) < (maxl))))
                {
                    continue;
                }
                if ((nodesTriviality) != (null))
                {
                    if (!(this.__check_for_nontrivial(pairs[i], termsToNodes, nodesTriviality)))
                    {
                        continue;
                    }
                }
                if ((l) == (maxl))
                {
                    indices.Add(i);
                }
                else
                {
                    indices = new() { i };
                    maxl = l;
                }
            }
            return indices;
        }

        public bool __check_for_nontrivial((int, HashSet<IndexWithMultitude>) nodeToTerms, List<HashSet<IndexWithMultitude>> termsToNodes, List<bool> nodesTriviality)
        {
            foreach (var pair in nodeToTerms.Item2)
            {
                var t = termsToNodes[pair.idx].Where(p => true).Select(p => new IndexWithMultitude(p.idx, p.multitude)).ToHashSet();
                t = this.__reduce_multitudes_corresponding_to_list(t, this.factorIndices);
                t = this.__reduce_multitudes_corresponding_to_list(t, this.prevFactorIndices);
                foreach (var p in t)
                {
                    if ((p.idx) == (nodeToTerms.Item1))
                    {
                        continue;
                    }
                    if (!(nodesTriviality[p.idx]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public HashSet<IndexWithMultitude> __reduce_multitudes_corresponding_to_list(HashSet<IndexWithMultitude> indicesWithMultitude, List<IndexWithMultitude> reductions)
        {
            foreach (var r in reductions)
            {
                List<IndexWithMultitude> res = indicesWithMultitude.Where(p => (p.idx) == (r.idx)).Select(p => p).ToList();
                Assert.True((res.Count()) == (1));
                res[0].multitude -= r.multitude;
                Assert.True((res[0].multitude) >= (0));
            }
            return indicesWithMultitude.Where(p => (p.multitude) > (0)).Select(p => p).ToHashSet();
        }

        public List<int> __get_largest_list_indices(List<List<int>> lists)
        {
            Assert.True((lists.Count()) > (0));
            List<int> indices = new() { 0 };
            var maxl = lists[0].Count();
            foreach (var i in Range.Get(1, lists.Count()))
            {
                var l = lists[i].Count();
                if ((l) == (maxl))
                {
                    indices.Add(i);
                }
                else
                {
                    if ((l) > (maxl))
                    {
                        indices = new() { i };
                        maxl = l;
                    }
                }
            }
            return indices;
        }

        public bool is_trivial()
        {
            return (this.children.Count()) == (0);
        }

        public void print(int indent = 0)
        {
            /*
            print(((((indent) * (" "))) + ("BATCH")));
            indent += 2;
            print(((((indent) * (" "))) + ("factors:")));
            foreach (var f in this.factorIndices)
            {
                print(((" ") + (str(f))));
            }
            print();
            print(((((indent) * (" "))) + ("atoms: ")));
            foreach (var a in this.atoms)
            {
                print(((" ") + (str(a))));
            }
            print();
            print(((((indent) * (" "))) + ("children: ")));
            foreach (var c in this.children)
            {
                c.print(((indent) + (2)));
            }
            */

            throw new InvalidOperationException();
        }
    }
}
