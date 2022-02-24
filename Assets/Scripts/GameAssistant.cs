using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace Checkers
{
    public class GameAssistant
    {
        private GameManager _manager;

        public GameAssistant(GameManager man) { _manager = man; }
        public void UpdatePairs(IEnumerable<BaseClickComponent> _cells, IEnumerable<BaseClickComponent> _chips)
        {
            // probably a bad idea but it works
            // uses transform

            // first we set up chips' pairs
            foreach (var chip in _chips)
            {
                if (chip == null) return;
                chip.Pair = null;
                foreach (var cell in _cells)
                {
                    if (cell.transform.localPosition == chip.transform.localPosition)
                    {
                        chip.Pair = cell;
                    }
                }
            }
            // now cells'
            foreach (var cell in _cells)
            {
                if (cell == null) return;
                cell.Pair = null;
                foreach (var chip in _chips)
                {
                    if (chip.transform.localPosition == cell.transform.localPosition)
                    {
                        cell.Pair = chip;
                    }
                }
            }
        }
        public void SetUpCellsNeighbors(IEnumerable<CellComponent> _cells)
        {        // uses transforms to set up Pair
            foreach (var cell in _cells)
            {
                Dictionary<NeighborType, CellComponent> cNeighb =
                    new Dictionary<NeighborType, CellComponent>()
                    {
                        { NeighborType.TopLeft, null },
                        { NeighborType.TopRight, null },
                        { NeighborType.BottomLeft, null },
                        { NeighborType.BottomRight, null }
                    };

                // determine positions of potential neighbors
                var pos = cell.gameObject.transform.localPosition;
                var topleft = pos + new Vector3(-1, 0, 1);
                var topright = pos + new Vector3(1, 0, 1);
                var botleft = pos + new Vector3(-1, 0, -1);
                var botright = pos + new Vector3(1, 0, -1);

                // kinda bruteforcing here but w/e it works and is only called once on start

                foreach (var lookup in _cells)
                {
                    var ccpos = lookup.gameObject.transform.localPosition;

                    if (ccpos == topleft)
                    {
                        cNeighb[NeighborType.TopLeft] = lookup;
                    }
                    if (ccpos == topright)
                    {
                        cNeighb[NeighborType.TopRight] = lookup;
                    }
                    if (ccpos == botleft)
                    {
                        cNeighb[NeighborType.BottomLeft] = lookup;
                    }
                    if (ccpos == botright)
                    {
                        cNeighb[NeighborType.BottomRight] = lookup;
                    }
                }
                cell.Configuration(cNeighb);
            }
        }
        public void ColorItem(BaseClickComponent item, Material mat, bool IsSelect)
        {
            if (item == null) return;
            if (IsSelect) item.AddAdditionalMaterial(mat);
            else item.RemoveAdditionalMaterial();
        }


    }


}
