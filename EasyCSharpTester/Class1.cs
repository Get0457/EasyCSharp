﻿using EasyCSharp;

namespace EasyCSharpTester
{
    public partial class Class1
    {
        [Property(PropertyVisibility = PropertyVisibility.Public)]
        bool _TestProperty1;
        [Property(
            SetVisibility = PropertyVisibility.Protected)
        ]
        int _TestProperty2;
        [AutoNotify]
        bool _Test2;

        public void Proc()
        {
            TestProperty1 = true;
            //Test2.
        }
    }
}