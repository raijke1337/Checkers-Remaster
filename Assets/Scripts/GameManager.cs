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
using static Checkers.Observer;

namespace Checkers {

    public class GameManager : MonoBehaviour, IPlayback
    {
        #region variables
        private List<CellComponent> _cells = new List<CellComponent>();
        private List<ChipComponent> _chips = new List<ChipComponent>();

        private HelperText _helper;

        private GameAssistant _assist;

        [SerializeField]
        private Material _selectM;
        [SerializeField, Tooltip("Time it takes for a chip to move")]
        private float _moveTime = 1f;

        [SerializeField, Space]
        private bool IsWhiteTurn = true;
        [SerializeField]
        private bool IsInputLocked = false;

        private bool IsGameComplete = false;


        private BaseClickComponent SelectedChip { get; set; }
        private Transform _camPivot;
        #endregion

        #region interface


        [SerializeField]
        public bool RecordForObserver = true;
        private bool LogicBypass = false;

        public void TakeCommand(string f, string t)
        {
            var from = GameObject.Find(f).GetComponent<BaseClickComponent>();
            var to = GameObject.Find(t).GetComponent<BaseClickComponent>();
            StartCoroutine(RequestMovement(from, to));
        }
        public void LockInput()
        {
            RecordForObserver = false;
            StartCoroutine(DumbLocker());
            LogicBypass = true;
        }
        private IEnumerator DumbLocker()
        { 
            while (true)
            {
                IsInputLocked = true;
                yield return null;
            }
        }

        public event GameEvents TurnEvent;

        #endregion

        #region main
        private void Start()
        {
            _helper = FindObjectOfType<HelperText>();
            _cells.AddRange(FindObjectsOfType<CellComponent>());
            _chips.AddRange(FindObjectsOfType<ChipComponent>());
            _assist = new GameAssistant(this);

            Subs();
            _assist.UpdatePairs(_cells, _chips);
            _assist.SetUpCellsNeighbors(_cells);

            _camPivot = Camera.main.GetComponentsInParent<Transform>().First(t => t.name == "CameraPivot");

        }

        private void C_OnFocusEventHandler(BaseClickComponent component, bool isSelect)
        {
            if (IsGameComplete) return;
            BaseClickComponent item;
            item = component;
            if (component == null) item = component.Pair;
            _assist.ColorItem(item, _selectM, isSelect);
        }

        private void C_OnClickEventHandler(BaseClickComponent component)
        {
            if (IsInputLocked) return;
            ClickingLogic(component);
        }

        private void ClickingLogic(BaseClickComponent component)
        {
 #region white_trun
            if (IsWhiteTurn)
            {
                if (SelectedChip == null)
                {
                    // select chip of appropriate color
                    if (component is ChipComponent && component.GetColor == ColorType.Black)
                    {
                        _helper.WriteInfo($"Select a white chip");
                        return;
                    }
                    if (component is ChipComponent && component.GetColor == ColorType.White)
                    {
                        SelectedChip = component;
                        _assist.ColorItem(component, _selectM, true);
                        _assist.ColorItem((component.Pair as CellComponent).GetNeighbors(NeighborType.TopRight), _selectM, true);
                        _assist.ColorItem((component.Pair as CellComponent).GetNeighbors(NeighborType.TopLeft), _selectM, true);

                        _helper.WriteInfo($"Selected chip {component.name}, click again to deselect");
                        return;
                    }
                    else { _helper.WriteInfo($"Clicked on {component.name}. It wasn't very effective..."); }
                }

                if (SelectedChip != null)
                {
                    // click on the same chip, deselect
                    if (SelectedChip == component)
                    {
                        SelectedChip = null;
                        _assist.ColorItem(component, _selectM, false);
                        _helper.WriteInfo($"Deselected chip {component.name}");
                        ClearMaterials();
                        return;
                    }

                    // click on something else

                    // empty cell, check if its near
                    else if (component.Pair == null && ((component as CellComponent) == (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.TopLeft) |
                        (component as CellComponent) == (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.TopRight)))
                    {
                        _helper.WriteInfo($"Movement: {SelectedChip} to {component.name}");
                        StartCoroutine(RequestMovement(SelectedChip, component));
                        return;
                    }
                    // occupied, clicked a paired something
                    else if (component.Pair != null)
                    {
                        // cant move to an occupied cell
                        if (component is CellComponent) { _helper.WriteInfo($"Can't move {SelectedChip} to {component.name}, it is occupied. Click on the chip to take it!"); return; }
                        // check color
                        if (component is ChipComponent && component.GetColor == ColorType.Black)
                        {
                            //check proximity
                            if ((component.Pair as CellComponent) == (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.TopLeft))
                            {
                                // next cell is empty
                                if ((component.Pair as CellComponent).GetNeighbors(NeighborType.TopLeft).Pair == null)
                                {
                                    StartCoroutine(RequestMovement(SelectedChip, component));
                                    _helper.WriteInfo($"{SelectedChip} attacking {component.name}");
                                }
                            }
                            if ((component.Pair as CellComponent) == (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.TopRight))
                            {
                                if ((component.Pair as CellComponent).GetNeighbors(NeighborType.TopRight).Pair == null)
                                {
                                    StartCoroutine(RequestMovement(SelectedChip,component));
                                    _helper.WriteInfo($"{SelectedChip} attacking {component.name}");
                                }
                            }
                            else _helper.WriteInfo($"{SelectedChip} can't take {component.name}");
                            return;
                        }
                        return;
                    }
                }
                _helper.WriteInfo("Something went wrong");
            }
#endregion
#region black_turn
            if (!IsWhiteTurn)
            {
                if (SelectedChip == null)
                {
                    // select chip of appropriate color
                    if (component is ChipComponent && component.GetColor == ColorType.White)
                    {
                        _helper.WriteInfo($"Select a black chip");
                        return;
                    }
                    if (component is ChipComponent && component.GetColor == ColorType.Black)
                    {
                        SelectedChip = component;
                        _assist.ColorItem(component, _selectM, true);
                        _assist.ColorItem((component.Pair as CellComponent).GetNeighbors(NeighborType.BottomRight), _selectM, true);
                        _assist.ColorItem((component.Pair as CellComponent).GetNeighbors(NeighborType.BottomLeft), _selectM, true);

                        _helper.WriteInfo($"Selected chip {component.name}, click again to deselect");
                        return;
                    }
                    else { _helper.WriteInfo($"Clicked on {component.name}. It wasn't very effective..."); }
                }

                if (SelectedChip != null)
                {
                    // click on the same chip, deselect
                    if (SelectedChip == component)
                    {
                        SelectedChip = null;
                        _assist.ColorItem(component, _selectM, false);
                        _helper.WriteInfo($"Deselected chip {component.name}");
                        ClearMaterials();
                        return;
                    }

                    // click on something else

                    // empty cell, check if its near
                    else if (component.Pair == null && ((component as CellComponent) == (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.BottomLeft) |
                        (component as CellComponent) == (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.BottomRight)))
                    {
                        _helper.WriteInfo($"Movement: {SelectedChip} to {component.name}");
                        StartCoroutine(RequestMovement(SelectedChip, component));
                        return;
                    }
                    // occupied, clicked a paired something
                    else if (component.Pair != null)
                    {
                        // cant move to an occupied cell
                        if (component is CellComponent) { _helper.WriteInfo($"Can't move {SelectedChip} to {component.name}, it is occupied. Click on the chip to take it!"); return; }
                        // check color
                        if (component is ChipComponent && component.GetColor == ColorType.White)
                        {
                            //check proximity
                            if ((component.Pair as CellComponent) == (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.BottomLeft))
                            {
                                // next cell is empty
                                if ((component.Pair as CellComponent).GetNeighbors(NeighborType.BottomLeft).Pair == null)
                                {
                                    StartCoroutine(RequestMovement(SelectedChip,component));
                                    _helper.WriteInfo($"{SelectedChip} attacking {component.name}");
                                }
                            }
                            if ((component.Pair as CellComponent) == (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.BottomRight))
                            {
                                if ((component.Pair as CellComponent).GetNeighbors(NeighborType.BottomRight).Pair == null)
                                {
                                    StartCoroutine(RequestMovement(SelectedChip, component));
                                    _helper.WriteInfo($"{SelectedChip} attacking {component.name}");
                                }
                            }
                            else _helper.WriteInfo($"{SelectedChip} can't take {component.name}");
                            return;
                        }
                        return;
                    }
                }
                _helper.WriteInfo("Something went wrong");
            }
#endregion
        }
        
        
        private IEnumerator RequestMovement(BaseClickComponent from, BaseClickComponent to)
        {
            if (RecordForObserver) { TurnEvent?.Invoke(from.name, to.name); }

            IsInputLocked = true;

            // we checked and can attack this chip (space behind is free)
            // yikes but should work with observer
            if (to is ChipComponent)
            {
                if ((from.Pair as CellComponent).GetNeighbors(NeighborType.TopLeft) == to.Pair as CellComponent)
                {
                    var newTgt = (to.Pair as CellComponent).GetNeighbors(NeighborType.TopLeft);
                    StartCoroutine(SmoothMove(from, newTgt, _moveTime));
                    _chips.Remove(to as ChipComponent);
                    Destroy(to.gameObject);
                }
                if ((from.Pair as CellComponent).GetNeighbors(NeighborType.TopRight) == to.Pair as CellComponent)
                {
                    var newTgt = (to.Pair as CellComponent).GetNeighbors(NeighborType.TopRight);
                    StartCoroutine(SmoothMove(from, newTgt, _moveTime));
                    _chips.Remove(to as ChipComponent);
                    Destroy(to.gameObject);
                }
                if ((from.Pair as CellComponent).GetNeighbors(NeighborType.BottomLeft) == to.Pair as CellComponent)
                {
                    var newTgt = (to.Pair as CellComponent).GetNeighbors(NeighborType.BottomLeft);
                    StartCoroutine(SmoothMove(from, newTgt, _moveTime));
                    _chips.Remove(to as ChipComponent);
                    Destroy(to.gameObject);
                }
                if ((from.Pair as CellComponent).GetNeighbors(NeighborType.BottomRight) == to.Pair as CellComponent)
                {
                    var newTgt = (to.Pair as CellComponent).GetNeighbors(NeighborType.BottomRight);
                    StartCoroutine(SmoothMove(from, newTgt, _moveTime));
                    _chips.Remove(to as ChipComponent);
                    Destroy(to.gameObject);
                }
            }
            // move to empty cell
            if (to is CellComponent)
            {
                StartCoroutine(SmoothMove(from, to, _moveTime));
            }

            yield return new WaitForSeconds(_moveTime);
            StartCoroutine(SmoothRotateCamera());
            NextTurnPlease();
        }

        //moves the chip, kinda broken but ok
        private IEnumerator SmoothMove(BaseClickComponent from, BaseClickComponent to, float time)
        {
            var start = from.transform.position;
            float progress = 0;
            while (progress < time)
            {
                progress += Time.deltaTime;
                from.transform.position = Vector3.Lerp
                    (start, to.transform.position, progress);

                _helper.WriteInfo($"Moving {from.name} to {to}, progress {Math.Round(progress,2)}");

                yield return null;

                // no idea why it just broke
            }
        }
        private IEnumerator SmoothRotateCamera()
        {
            Quaternion stRot = _camPivot.transform.rotation;
            Quaternion tgtRot = Quaternion.Euler(0, stRot.eulerAngles.y + 180f, 0);

            float progress = 0;
            while (progress < _moveTime)
            {
                progress += Time.deltaTime;

                _camPivot.rotation = Quaternion.Slerp(stRot, tgtRot, progress);

                yield return null;
            }
            yield return null;
        }
//

        // update materials, pairs, reset selected chip, switch bool, unlock input
        // also check winner
        private void NextTurnPlease()
        {
            ClearMaterials();
            _assist.UpdatePairs(_cells, _chips);

            if (CheckWinConditions()) return; 

            SelectedChip = null;
            IsWhiteTurn = !IsWhiteTurn;
            IsInputLocked = false;
        }
        private void ClearMaterials()
        {
            foreach (var cell in _cells) cell.RemoveAdditionalMaterial();
            foreach (var chip in _chips) chip.RemoveAdditionalMaterial();
        }
        private bool CheckWinConditions()
        {
            if (LogicBypass) return false;

            if (IsWhiteTurn)
            {
                if (((SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.TopRight) == null &&
                    (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.TopLeft) == null) ||
                        !(_chips.Exists(t => t.GetColor == ColorType.Black)))
                {
                    _helper.WriteInfo("White side wins! Restart the game manually.");
                    return true;
                }
                else return false;
            }
            if (!IsWhiteTurn)
            {
                if (((SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.BottomLeft) == null &&
                    (SelectedChip.Pair as CellComponent).GetNeighbors(NeighborType.BottomRight) == null) ||
                        !(_chips.Exists(t => t.GetColor == ColorType.White)))
                {
                    _helper.WriteInfo("Black side wins! Restart the game manually.");
                    return true;
                }
                else return false;
            }
            else return false;
        }

        private void OnDisable()
        {
            Subs(false);
        }
        // subs to component events
        private void Subs(bool isStarting = true)
        {
            if (isStarting)
            {
                foreach (var c in _chips)
                {
                    c.OnClickEventHandler += C_OnClickEventHandler;
                    c.OnFocusEventHandler += C_OnFocusEventHandler;
                }
                foreach (var c in _cells)
                {
                    c.OnClickEventHandler += C_OnClickEventHandler;
                    c.OnFocusEventHandler += C_OnFocusEventHandler;
                }
            }
            else
            {
                foreach (var c in _chips)
                {
                    c.OnClickEventHandler -= C_OnClickEventHandler;
                    c.OnFocusEventHandler -= C_OnFocusEventHandler;
                }
                foreach (var c in _cells)
                {
                    c.OnClickEventHandler -= C_OnClickEventHandler;
                    c.OnFocusEventHandler -= C_OnFocusEventHandler;
                }
            }
        }
        #endregion

        #region editor
#if UNITY_EDITOR
        [ContextMenu("Clean up unused white cells")]
        public void Clean()
        {
            var cells = FindObjectsOfType<CellComponent>();
            foreach (var cell in cells)
            {
                if (cell.GetColor == ColorType.White)
                {
                    DestroyImmediate(cell);
                }
            }
        }

#endif
        #endregion

    }
}