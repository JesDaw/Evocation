using UnityEngine;
using UnityEngine.Events;

public class MapZoneMaster : MonoBehaviour
{
    [SerializeField] GameObject UpperZone;
    [SerializeField] GameObject MiddleZone;
    [SerializeField] GameObject LowerZone;

    [SerializeField] UnityEvent _loose_game;
    [SerializeField] UnityEvent _win_game;

    int _player_score;
    int _total_zones;
    public void CompareAllZones()
    {
        if (UpperZone != null)
        {
            if (UpperZone.TryGetComponent<MapZonesManager>(out MapZonesManager _upper_manager))
            {
                _total_zones++;
                if (_upper_manager.CompareSides()) { _player_score++; }
            }
        }

        if (MiddleZone != null)
        {
            if (MiddleZone.TryGetComponent<MapZonesManager>(out MapZonesManager _middle_manager))
            {
                _total_zones++;
                if (_middle_manager.CompareSides()) { _player_score++; }
            }
        }

        if (LowerZone != null)
        {
            if (LowerZone.TryGetComponent<MapZonesManager>(out MapZonesManager _lower_manager))
            {
                _total_zones++;
                if (_lower_manager.CompareSides()) { _player_score++; }
            }
        }
        
        //Debug.Log(_player_score + " " + _total_zones);

        if (_player_score >= 1){ _win_game.Invoke(); }
        else{ _loose_game.Invoke(); }
    }
}
