using UnityEngine;
using UnityEngine.InputSystem;

public class FreeMovement : MonoBehaviour
{
    public float MoveSpeed = 10;

    public float RotateSpeed = 10;

    private InputAction _horizontalMoveAction;

    private InputAction _verticalMoveAction;

    private InputAction _sprintAction;

    private InputAction _rotateAction;

    private InputAction _startRotateAction;

    private Keyboard _keyboard;

    protected void Start()
    {
        _horizontalMoveAction = InputSystem.actions["Move"];
        _verticalMoveAction = InputSystem.actions["Vertical Move"];
        _sprintAction = InputSystem.actions["Sprint"];
        _rotateAction = InputSystem.actions["Look"];
    }

    protected void Update()
    {
        Move(_horizontalMoveAction.ReadValue<Vector2>());
        VerticalMove(_verticalMoveAction.ReadValue<float>());
        Rotate(_rotateAction.ReadValue<Vector2>());
    }

    private void Move(Vector2 inputDir)
    {
        var moveDir = new Vector3(inputDir.x, 0, inputDir.y);
        var movementModifier = Time.deltaTime * MoveSpeed * SpeedModifier;
        transform.position += transform.rotation * moveDir * movementModifier;
    }

    private void VerticalMove(float inputDir)
    {
        var position = transform.position;
        position.y += inputDir * Time.deltaTime * MoveSpeed * SpeedModifier;
        transform.position = position;
    }

    private void Rotate(Vector2 inputDir)
    {
        if (_keyboard?.leftCtrlKey.IsPressed() ?? false)
        {
            return;
        }

        var rotateDir = new Vector3(-inputDir.y, inputDir.x, 0);
        transform.eulerAngles += Time.deltaTime * RotateSpeed * rotateDir;
    }

    private int SpeedModifier => _sprintAction.IsPressed() ? 2 : 1;
}