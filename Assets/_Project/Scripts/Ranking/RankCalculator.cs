using System;
using UnityEngine;

public static class RankCalculator
{
    private const int DefaultKFactor = 32;

    public static int CalculateRatingChange(
        int playerRating,
        int opponentRating,
        bool playerWon)
    {
        double ratingDifference =
            opponentRating - playerRating;

        double expectedResult =
            1.0 /
            (
                1.0 +
                Math.Pow(
                    10.0,
                    ratingDifference / 400.0
                )
            );

        double actualResult =
            playerWon ? 1.0 : 0.0;

        double change =
            DefaultKFactor *
            (actualResult - expectedResult);

        return Mathf.RoundToInt(
            (float)change
        );
    }

    public static RankTier GetTier(int rating)
    {
        if (rating >= 1400)
            return RankTier.Diamond;

        if (rating >= 1200)
            return RankTier.Gold;

        if (rating >= 1000)
            return RankTier.Silver;

        return RankTier.Bronze;
    }
}