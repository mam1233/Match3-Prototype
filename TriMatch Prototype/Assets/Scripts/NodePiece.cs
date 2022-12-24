using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class NodePiece : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Point index;
    public int val;

    [HideInInspector]
    public Vector2 pos;
    [HideInInspector]
    public RectTransform rect;

    bool isUpdating;

    Image img;

    public void Initialize(int v, Point p, Sprite piece)
    {
        img = GetComponent<Image>();
        rect = GetComponent<RectTransform>();

        val = v;
        SetIndex(p);
        img.sprite = piece;
    }

    public void SetIndex(Point p)
    {
        index = p;
        ResetPosition();
        UpdateName();
    }

    public void ResetPosition()
    {
        pos = new Vector2(32 + (64 * index.x), -32 - (64 * index.y));
    }

    void UpdateName()
    {
        transform.name = $"Node [{index.x}],[{index.y}]";
    }
    public void MovePosition(Vector2 move)
    {
        rect.anchoredPosition += move * Time.deltaTime * 16;
    }
    public void MovePositionTo(Vector2 pos)
    {
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, pos, 16f * Time.deltaTime);
    }

    public bool UpdatePiece()
    {
        if (Vector3.Distance(rect.anchoredPosition, pos) > 1)
        {
            MovePositionTo(pos);
            isUpdating = true;
            return true;
        }
        else
        {
            rect.anchoredPosition = pos;
            isUpdating = false;
            return false;
        }

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isUpdating) return;
        MovePieces.instance.MovePiece(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        MovePieces.instance.DropPiece();
    }
}
