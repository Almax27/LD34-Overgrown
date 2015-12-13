using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace FuncWorks.Unity.UTiled {
    [System.Serializable]
    public class UTiledProperty {
        [SerializeField]
        public string Name;
        [SerializeField]
        public string Value;
    }

    public class UTiledProperties : MonoBehaviour {

        [SerializeField]
        List<UTiledProperty> _props = new List<UTiledProperty>();

        public string[] Names { get { return _props.Select(x => x.Name).ToArray(); } }
        public string[] Values { get { return _props.Select(x => x.Value).ToArray(); } }
        public UTiledProperty[] Properties { get { return _props.ToArray(); } }

        [SerializeField]
        public string ObjectType = null;

        int? _firstIndexOf(string Name) {
            int i = _props.IndexOf(_props.FirstOrDefault(x => x.Name.Equals(Name)));
            return i >= 0 ? (int?)i : null;
        }

        public void Add(string Name, string Value) {
            _props.Add(new UTiledProperty() { Name = Name, Value = Value });
        }

        public void SetValue(int Index, string Value) {
            if (_props.Count > Index && Index >= 0)
                _props[Index].Value = Value;
        }

        public void SetValue(string Name, string Value) {
            int? i = _firstIndexOf(Name);

            if (i.HasValue)
                _props[i.Value].Value = Value;
        }

        public void RenameProperty(int Index, string NewKey) {
            if (_props.Count > Index && Index >= 0)
                _props[Index].Name = NewKey;
        }

        public void DeleteProperty(int Index) {
            if (_props.Count > Index && Index >= 0) 
                _props.RemoveAt(Index);
        }

        public bool HasPropertyNamed(string Name) {
            return _firstIndexOf(Name).HasValue;
        }

        public string GetValue(string Name) {
            int? i = _firstIndexOf(Name);
            return i.HasValue ? _props[i.Value].Value : null;
        }

        public string[] GetValues(string Name) {
            int? i = _firstIndexOf(Name);
            return i.HasValue ? _props.Where(x => name.Equals(Name)).Select(x => x.Value).ToArray() : null;
        }

        public bool? GetValueAsBool(string Name) {
            int? i = _firstIndexOf(Name);
            return i.HasValue ? (bool?)System.Convert.ToBoolean(_props[i.Value].Value) : null;
        }

        public float? GetValueAsFloat(string Name) {
            int? i = _firstIndexOf(Name);
            return i.HasValue ? (float?)System.Convert.ToSingle(_props[i.Value].Value) : null;
        }

        public int? GetValueAsInt(string Name) {
            int? i = _firstIndexOf(Name);
            return i.HasValue ? (int?)System.Convert.ToInt32(_props[i.Value].Value) : null;
        }
    }
}
