using System.Collections.Generic;
public class ProjectileMapping {
    private List<int> _keys;
    private List<ulong> _values;

    public ProjectileMapping(Dictionary<int, ulong> dictionary) {
        _keys = new List<int>(dictionary.Keys);
        _values = new List<ulong>(dictionary.Values);
    }

    public Dictionary<int, ulong> ToDictionary() {
        var dictionary = new Dictionary<int, ulong>();
        for (var i = 0; i < _keys.Count; i++) dictionary.Add(_keys[i], _values[i]);
        return dictionary;
    }
}
