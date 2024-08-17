using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CustomElements {
    [UxmlElement]
    public partial class ExampleVisualElement : VisualElement
    {
        [UxmlAttribute]
        public string myStringValue { get; set; }

        [UxmlAttribute]
        public int myIntValue { get; set; }

        [UxmlAttribute]
        public float myFloatValue { get; set; }

        [UxmlAttribute]
        public List<int> myListOfInts { get; set; }

        [UxmlAttribute, UxmlTypeReference(typeof(VisualElement))]
        public Type myType { get; set; }

        [UxmlAttribute]
        public Texture2D myTexture { get; set; }

        [UxmlAttribute]
        public Color myColor { get; set; }
    }
}

