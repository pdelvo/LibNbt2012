﻿using System;
using System.IO;
using NUnit.Framework;

namespace LibNbt.Test {
    [TestFixture]
    public sealed class CompoundTests {
        const string TempDir = "TestTemp";


        [SetUp]
        public void CompoundTestsSetup() {
            Directory.CreateDirectory( TempDir );
        }


        [Test]
        public void InitializingCompoundFromCollectionTest() {
            NbtTag[] allNamed = new NbtTag[] {
                new NbtShort( "allNamed1", 1 ),
                new NbtLong( "allNamed2", 2 ),
                new NbtInt( "allNamed3", 3 )
            };

            NbtTag[] someUnnamed = new NbtTag[] {
                new NbtInt( "someUnnamed1", 1 ),
                new NbtInt( 2 ),
                new NbtInt( "someUnnamed3", 3 )
            };

            NbtTag[] someNull = new NbtTag[] {
                new NbtInt( "someNull1", 1 ),
                null,
                new NbtInt( "someNull3", 3 )
            };

            NbtTag[] dupeNames = new NbtTag[] {
                new NbtInt( "dupeNames1", 1 ),
                new NbtInt( "dupeNames2", 2 ),
                new NbtInt( "dupeNames1", 3 )
            };

            // null collection, should throw
            Assert.Throws<ArgumentNullException>( () => new NbtCompound( "nullTest", null ) );

            // proper initialization
            Assert.DoesNotThrow( () => new NbtCompound( "allNamedTest", allNamed ) );
            CollectionAssert.AreEquivalent( allNamed, new NbtCompound( "allNamedTest", allNamed ) );

            // some tags are unnamed, should throw
            Assert.Throws<ArgumentException>( () => new NbtCompound( "someUnnamedTest", someUnnamed ) );

            // some tags are null, should throw
            Assert.Throws<ArgumentNullException>( () => new NbtCompound( "someNullTest", someNull ) );

            // some tags have same names, should throw
            Assert.Throws<ArgumentException>( () => new NbtCompound( "dupeNamesTest", dupeNames ) );
        }


        [Test]
        public void GettersAndSetters() {
            NbtCompound parent = new NbtCompound( "Parent" );
            NbtCompound child = new NbtCompound( "Child" );
            NbtCompound nestedChild = new NbtCompound( "NestedChild" );
            NbtList childList = new NbtList( "ChildList" );
            NbtList nestedChildList = new NbtList( "NestedChildList" );
            NbtInt testInt = new NbtInt( 1 );
            childList.Add( testInt );
            nestedChildList.Add( testInt );
            parent.Add( child );
            parent.Add( childList );
            child.Add( nestedChild );
            child.Add( nestedChildList );

            // Accessing nested compound tags using indexers
            Assert.AreEqual( parent["Child"]["NestedChild"], nestedChild );
            Assert.AreEqual( parent["Child"]["NestedChildList"], nestedChildList );
            Assert.AreEqual( parent["Child"]["NestedChildList"][0], testInt );

            // Accessing nested compound tags using Get<T>
            Assert.AreEqual( parent.Get<NbtCompound>( "Child" ).Get<NbtCompound>( "NestedChild" ), nestedChild );
            Assert.AreEqual( parent.Get<NbtCompound>( "Child" ).Get<NbtList>( "NestedChildList" ), nestedChildList );
            Assert.AreEqual( parent.Get<NbtCompound>( "Child" ).Get<NbtList>( "NestedChildList" )[0], testInt );

            // Accessing with Get<T> and an invalid given type
            Assert.Throws<InvalidCastException>( () => parent.Get<NbtInt>( "Child" ) );

            // Trying to use integer indexers on non-NbtList tags
            Assert.Throws<InvalidOperationException>( () => parent[0] = testInt );
            Assert.Throws<InvalidOperationException>( () => testInt[0] = testInt );

            // Trying to use string indexers on non-NbtCompound tags
            Assert.Throws<InvalidOperationException>( () => childList["test"] = testInt );
            Assert.Throws<InvalidOperationException>( () => testInt["test"] = testInt );

            // Trying to get a non-existent element by name
            Assert.IsNull( parent.Get<NbtTag>( "NonExistentTag" ) );
            Assert.IsNull( parent["NonExistentTag"] );

            // Null indices on NbtCompound
            Assert.Throws<ArgumentNullException>( () => parent.Get<NbtTag>( null ) );
            Assert.Throws<ArgumentNullException>( () => parent[null] = testInt );
            Assert.Throws<ArgumentNullException>( () => testInt = (NbtInt)parent[null] );

            // Out-of-range indices on NbtList
            Assert.Throws<ArgumentOutOfRangeException>( () => testInt = (NbtInt)childList[-1] );
            Assert.Throws<ArgumentOutOfRangeException>( () => childList[-1] = testInt );
            Assert.Throws<ArgumentOutOfRangeException>( () => testInt = childList.Get<NbtInt>( -1 ) );
            Assert.Throws<ArgumentOutOfRangeException>( () => testInt = (NbtInt)childList[childList.Count] );
            Assert.Throws<ArgumentOutOfRangeException>( () => testInt = childList.Get<NbtInt>( childList.Count ) );
        }


        [Test]
        public void AddingAndRemoving() {
            NbtCompound test = new NbtCompound();

            NbtInt foo =  new NbtInt( "Foo" );

            test.Add( foo );

            // adding duplicate object
            Assert.Throws<ArgumentException>( () => test.Add( foo ) );

            // adding duplicate name
            Assert.Throws<ArgumentException>( () => test.Add( new NbtByte( "Foo" ) ) );

            // adding unnamed tag
            Assert.Throws<ArgumentException>( () => test.Add( new NbtInt() ) );

            // adding null
            Assert.Throws<ArgumentNullException>( () => test.Add( null ) );

            // contains existing name
            Assert.IsTrue( test.Contains( "Foo" ) );

            // contains existing object
            Assert.IsTrue( test.Contains( foo ) );

            // contains non-existent name
            Assert.IsFalse( test.Contains( "Bar" ) );

            // contains existing name / different object
            Assert.IsFalse( test.Contains( new NbtInt( "Foo" ) ) );

            // removing non-existent name
            Assert.IsFalse( test.Remove( "Bar" ) );

            // removing existing name
            Assert.IsTrue( test.Remove( "Foo" ) );

            // removing non-existent name
            Assert.IsFalse( test.Remove( "Foo" ) );

            // re-adding object
            test.Add( foo );

            // removing existing object
            Assert.IsTrue( test.Remove( foo ) );

            // clearing an empty NbtCompound
            Assert.AreEqual( test.Count, 0 );
            test.Clear();

            // re-adding after clearing
            test.Add( foo );
            Assert.AreEqual( test.Count, 1 );

            // clearing a non-empty NbtCompound
            test.Clear();
            Assert.AreEqual( test.Count, 0 );
        }


        [Test]
        public void UtilityMethods() {
            NbtTag[] testThings = new NbtTag[] {
                new NbtShort( "Name1", 1 ),
                new NbtInt( "Name2", 2 ),
                new NbtLong( "Name3", 3 )
            };
            NbtCompound compound = new NbtCompound();

            // add range
            compound.AddRange( testThings );

            // add range with duplicates
            Assert.Throws<ArgumentException>( () => compound.AddRange( testThings ) );


        }


        [TearDown]
        public void CompoundTestsTearDown() {
            if( Directory.Exists( TempDir ) ) {
                foreach( var file in Directory.GetFiles( TempDir ) ) {
                    File.Delete( file );
                }
                Directory.Delete( TempDir );
            }
        }
    }
}