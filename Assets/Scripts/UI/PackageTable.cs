using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PackageProject/PackageTable", fileName = "PackageTable")]
public class PackageTable : ScriptableObject
{
    public List<PackageTableItem> DataList = new List<PackageTableItem>();
}

[System.Serializable]
public class PackageTableItem
{
    public int id;
    public int type;
    public int star;
    public string name;
    public string description;
    public string skillsDescription;
    public string imagePath;
    public string modelPath;
}
