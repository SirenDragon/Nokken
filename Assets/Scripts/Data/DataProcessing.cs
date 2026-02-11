using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DataProcessing
{
    public int deaths;
    public int successfulAttacks;
    public int generatorsFixed;
    public int timesAttacked;

    public DataProcessing(int deaths, int successfulAttacks, int generatorsFixed, int timesAttacked)
    {
        this.deaths = deaths;
        this.successfulAttacks = successfulAttacks;
        this.generatorsFixed = generatorsFixed;
        this.timesAttacked = timesAttacked;
    }
}
