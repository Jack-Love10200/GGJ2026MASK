using System;
using System.Collections.Generic;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    // Struct/ Class definitions ////////////////////////////////////////////////////////////////
    class ComboStats
    {
        public int totalKills = 0;
    }



    // Public variables ////////////////////////////////////////////////////////////////////////
    public Action mOnComboValueChanged;
    public Action mOnComboLevelChanged;

    // Getters and setters /////////////////////////////////////////////////////////////////////

    // Private variables ///////////////////////////////////////////////////////////////////////

    private ComboStats mCurrComboStats = new ComboStats();

    float mCurrComboValue = 0.0f;

    int mCurrComboLevel = -1;


    //[SerializeField]
    //AnimationCurve

    [SerializeField]
    private ComboLevelSettings mComboLevelSettings;

    float mDebugComboIncreaseRate = 200.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        bool comboLevelChanged = RecalculateComboLevel();

        if (comboLevelChanged)
        {
            if (mOnComboLevelChanged != null)
            {
                mOnComboLevelChanged.Invoke();
            }
        }

        // For debugging, increase value always over time. TODO: remove or put this under a debug bool
        IncreaseComboValue(mDebugComboIncreaseRate * Time.deltaTime);
    }

    // Public interface

    public void TrackEnemyFinished(int numEnemiesFinished)
    {
        mCurrComboStats.totalKills += numEnemiesFinished;


        // TODO: change this to factor in the specific value for whatever enemy was finished
        IncreaseComboValue(numEnemiesFinished);
    }

    
    public string GetCurrentComboLevelName()
    {
        if (mComboLevelSettings.mComboLevelNames.Count < mCurrComboLevel)
        {
            print("ComboMamager: GetCurrentComboLevelName: No combo level of level " + mCurrComboLevel.ToString() +" exists. Need to add more combos");
        }
        return mComboLevelSettings.mComboLevelNames[mCurrComboLevel];
    }
    // Private functions

    void IncreaseComboValue(float amountToIncreaseCombo)
    {
        mCurrComboValue += amountToIncreaseCombo;

        if (mOnComboValueChanged != null)
        {
            mOnComboValueChanged.Invoke();

        }
    }

    // Recalculates combo level
    // returns true if combo level changed, false otherwise
    bool RecalculateComboLevel()
    {
        int lastComboLevel = mCurrComboLevel;
        mCurrComboLevel = 0;

        // Find current combo level
        for (int i = 0; i < mComboLevelSettings.mComboLevelThresholds.Count; i++)
        {
            if (mCurrComboValue >= mComboLevelSettings.mComboLevelThresholds[i])
            {
                mCurrComboLevel = i;
            }
            else
            {
                break;
            }
        }

        return lastComboLevel != mCurrComboLevel;

    }

}







// Variable sections template

// Struct/ Class definitions ////////////////////////////////////////////////////////////////




// Public variables ////////////////////////////////////////////////////////////////////////

// Getters and setters /////////////////////////////////////////////////////////////////////

// Private variables ///////////////////////////////////////////////////////////////////////
