using System;

public enum CardinalDirection { Right, Up, Left, Down }

public static class CardinalAim
{
    public static CardinalDirection FromMovement(float x, float y, CardinalDirection previous)
    {
        if (x == 0 && y == 0) return previous;
        float horizontal = Math.Abs(x), vertical = Math.Abs(y);
        CardinalDirection xDirection = x >= 0 ? CardinalDirection.Right : CardinalDirection.Left;
        CardinalDirection yDirection = y >= 0 ? CardinalDirection.Up : CardinalDirection.Down;
        if (horizontal == vertical)
        {
            if (previous == xDirection || previous == yDirection) return previous;
            return xDirection; // Deterministic when the old direction is outside both sectors.
        }
        return horizontal > vertical ? xDirection : yDirection;
    }

    public static CardinalDirection Step(float x, float y, CardinalDirection previous, bool firing)
    {
        return firing ? previous : FromMovement(x, y, previous);
    }
}
