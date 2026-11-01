using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tornado))]
public sealed class PlayerController : MonoBehaviour
{
    private const float JoystickRadius = 145f;
    private const float DeadZone = 0.055f;
    private const float MaximumTouchHeight = 0.72f;
    private const float InputPower = 0.82f;

    private const float BackgroundOpacity = 0.20f;
    private const float HandleOpacity = 0.62f;

    private const float BackgroundScale = 2f;
    private const float HandleScale = 0.68f;

    private Tornado tornado;

    private int joystickFingerId = -1;

    private Vector2 joystickStart;
    private Vector2 joystickCurrent;

    private bool joystickActive;

    private Texture2D whiteTexture;


    private void Awake()
    {
        tornado =
            GetComponent<Tornado>();

        CreateWhiteTexture();
    }


    private void Update()
    {
        if (tornado == null ||
            tornado.IsDefeated)
        {
            return;
        }

        Vector2 input;

        if (Input.touchCount > 0 ||
            joystickActive)
        {
            input =
                GetTouchInput();
        }
        else
        {
            input =
                GetKeyboardInput();
        }

        Vector3 direction =
            new Vector3(
                input.x,
                0f,
                input.y
            );

        tornado.SetMoveIntent(
            direction,
            input.magnitude
        );
    }


    private Vector2 GetTouchInput()
    {
        if (!joystickActive)
        {
            FindJoystickTouch();
        }

        if (!joystickActive)
        {
            return Vector2.zero;
        }

        Touch trackedTouch =
            default;

        bool foundFinger =
            false;

        for (int i = 0;
             i < Input.touchCount;
             i++)
        {
            Touch touch =
                Input.GetTouch(i);

            if (touch.fingerId !=
                joystickFingerId)
            {
                continue;
            }

            trackedTouch =
                touch;

            foundFinger =
                true;

            break;
        }

        if (!foundFinger)
        {
            StopJoystick();

            return Vector2.zero;
        }

        if (trackedTouch.phase ==
                TouchPhase.Ended ||
            trackedTouch.phase ==
                TouchPhase.Canceled)
        {
            StopJoystick();

            return Vector2.zero;
        }

        joystickCurrent =
            trackedTouch.position;

        Vector2 difference =
            joystickCurrent -
            joystickStart;

        float distance =
            difference.magnitude;

        if (distance <=
            JoystickRadius *
            DeadZone)
        {
            return Vector2.zero;
        }

        float rawMagnitude =
            Mathf.Clamp01(
                distance /
                JoystickRadius
            );

        float magnitude =
            Mathf.InverseLerp(
                DeadZone,
                1f,
                rawMagnitude
            );

        magnitude =
            Mathf.Pow(
                magnitude,
                InputPower
            );

        Vector2 direction =
            difference /
            Mathf.Max(
                0.001f,
                distance
            );

        return
            direction *
            magnitude;
    }


    private void FindJoystickTouch()
    {
        float maximumY =
            Screen.height *
            MaximumTouchHeight;

        for (int i = 0;
             i < Input.touchCount;
             i++)
        {
            Touch touch =
                Input.GetTouch(i);

            if (touch.phase !=
                TouchPhase.Began)
            {
                continue;
            }

            if (touch.position.y >
                maximumY)
            {
                continue;
            }

            joystickFingerId =
                touch.fingerId;

            joystickStart =
                touch.position;

            joystickCurrent =
                touch.position;

            joystickActive =
                true;

            return;
        }
    }


    private void StopJoystick()
    {
        joystickActive =
            false;

        joystickFingerId =
            -1;

        joystickStart =
            Vector2.zero;

        joystickCurrent =
            Vector2.zero;
    }


    private static Vector2 GetKeyboardInput()
    {
        Vector2 input =
            new Vector2(
                Input.GetAxisRaw(
                    "Horizontal"
                ),
                Input.GetAxisRaw(
                    "Vertical"
                )
            );

        return
            Vector2.ClampMagnitude(
                input,
                1f
            );
    }


    private void CreateWhiteTexture()
    {
        whiteTexture =
            new Texture2D(
                1,
                1,
                TextureFormat.RGBA32,
                false
            );

        whiteTexture.name =
            "RuntimeJoystickTexture";

        whiteTexture.SetPixel(
            0,
            0,
            Color.white
        );

        whiteTexture.Apply(
            false,
            true
        );
    }


    private void OnGUI()
    {
        if (!joystickActive ||
            whiteTexture == null)
        {
            return;
        }

        Vector2 difference =
            joystickCurrent -
            joystickStart;

        difference =
            Vector2.ClampMagnitude(
                difference,
                JoystickRadius
            );

        Vector2 handlePosition =
            joystickStart +
            difference;

        Vector2 guiStart =
            ScreenToGUI(
                joystickStart
            );

        Vector2 guiHandle =
            ScreenToGUI(
                handlePosition
            );

        float backgroundSize =
            JoystickRadius *
            BackgroundScale;

        float handleSize =
            JoystickRadius *
            HandleScale;

        Color previousColor =
            GUI.color;

        GUI.color =
            new Color(
                1f,
                1f,
                1f,
                BackgroundOpacity
            );

        GUI.DrawTexture(
            new Rect(
                guiStart.x -
                    backgroundSize * 0.5f,

                guiStart.y -
                    backgroundSize * 0.5f,

                backgroundSize,
                backgroundSize
            ),
            whiteTexture
        );

        GUI.color =
            new Color(
                1f,
                1f,
                1f,
                HandleOpacity
            );

        GUI.DrawTexture(
            new Rect(
                guiHandle.x -
                    handleSize * 0.5f,

                guiHandle.y -
                    handleSize * 0.5f,

                handleSize,
                handleSize
            ),
            whiteTexture
        );

        GUI.color =
            previousColor;
    }


    private static Vector2 ScreenToGUI(
        Vector2 screenPosition)
    {
        return
            new Vector2(
                screenPosition.x,
                Screen.height -
                screenPosition.y
            );
    }


    private void OnDisable()
    {
        StopJoystick();

        if (tornado != null &&
            !tornado.IsDefeated)
        {
            tornado.SetMoveIntent(
                Vector3.zero,
                0f
            );
        }
    }


    private void OnDestroy()
    {
        if (whiteTexture != null)
        {
            Destroy(
                whiteTexture
            );
        }
    }
}