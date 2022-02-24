using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;

namespace Checkers
{
    public class Observer : MonoBehaviour
    {
        public delegate void GameEvents(string from, string to);
        private IPlayback _man;
        public interface IPlayback
        {
            void TakeCommand(string from, string to);
            event GameEvents TurnEvent;
            void LockInput();
        }

        [SerializeField]
        public bool IsPlayingRecord = false;
        [SerializeField, Tooltip("Time between turns")]
        private float _waitTime = 2f;
        string path;

        BinaryFormatter form = new BinaryFormatter();

        [Serializable]
        public struct GameTurn
        {
            public string from;
            public string to;
            public GameTurn(string f, string t)
            {
                from = f; to = t;
            }
        }
        private List<GameTurn> GameRecord = new List<GameTurn>();

        private async void Start()
        {
            await Task.Yield();
            // my new favorite bandaid

            path = Application.dataPath + "/ObserverRecording.txt";
            _man = FindObjectOfType<GameManager>();
            // questionable dunno
            // kinda missing the point here?

            _man.TurnEvent += Observer_TurnEvent;

            if (IsPlayingRecord)
            {
                _man.LockInput();
                GameRecord = ReadRecord();
                StartCoroutine(Playing());
            }
        }

        private void Observer_TurnEvent(string from, string to)
        {
            GameRecord.Add(new GameTurn(from, to));
            UpdateRecord();
        }

        private IEnumerator Playing()
        {
            int turn = 0;
            while (IsPlayingRecord)
            {
                if (turn >= GameRecord.Count) break;
                _man.TakeCommand(GameRecord[turn].from, GameRecord[turn].to);
                turn++;
                yield return new WaitForSeconds(_waitTime);
            }
        }


        private void UpdateRecord()
        {
            Debug.Log("Update record");
            var save = File.Create(path);
            save.Close();

            using (var stream = File.OpenWrite(path))
            {
                form.Serialize(stream, GameRecord);
            }
        }


        private List<GameTurn> ReadRecord()
        {
            if (File.Exists(path))
            {
                Debug.Log("Observer data found");
                using (var stream = File.OpenRead(path))
                {
                    return (List<GameTurn>)form.Deserialize(stream);
                }
            }
            else
            {
                Debug.LogError("No record found");
                return null;
            }
        }
    }
}