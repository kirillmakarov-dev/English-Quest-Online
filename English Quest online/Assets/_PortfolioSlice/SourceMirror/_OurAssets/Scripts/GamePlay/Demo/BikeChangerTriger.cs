using UnityEngine;
namespace App.CameraSystem
{
public class BikeChangerTriger : MonoBehaviour
{
    public string _playerTag = "Player";
    public bool _triggerOnEnter = true;

    public GameObject _bikeObject;
    public GameObject _playerObject;
    public GameObject _cameraToEnable;
    public GameObject _cameraToDisable;
    public bool _disableTriggerAfterUse = true;


    private void OnTriggerEnter(Collider other)
        {
            if (!_triggerOnEnter) return;
            
            if (!other.CompareTag(_playerTag)) return;

            if (_bikeObject != null)
                _bikeObject.SetActive(true);

            if (_playerObject != null)
                Destroy(_playerObject);
            else
                Destroy(other.transform.root.gameObject);

            if (_cameraToEnable != null)
                _cameraToEnable.SetActive(true);

            if (_cameraToDisable != null)
                _cameraToDisable.SetActive(false);

            if (_disableTriggerAfterUse)
                gameObject.SetActive(false);
        }
}
}
