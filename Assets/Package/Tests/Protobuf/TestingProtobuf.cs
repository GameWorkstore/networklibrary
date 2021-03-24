// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: TestingProtobuf.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Testing {

  /// <summary>Holder for reflection information generated from TestingProtobuf.proto</summary>
  public static partial class TestingProtobufReflection {

    #region Descriptor
    /// <summary>File descriptor for TestingProtobuf.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static TestingProtobufReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ChVUZXN0aW5nUHJvdG9idWYucHJvdG8SB1Rlc3RpbmciUQoPU2ltcGxlU3Ry",
            "dWN0dXJlEg4KBlZhbHVlMRgBIAEoBRIOCgZWYWx1ZTIYAiABKAMSDgoGVmFs",
            "dWUzGAMgASgCEg4KBlZhbHVlNBgEIAEoCSJKChBDb21wbGV4U3RydWN0dXJl",
            "Eg0KBVZhbHVlGAEgASgFEicKBE5leHQYAiABKAsyGS5UZXN0aW5nLkNvbXBs",
            "ZXhTdHJ1Y3R1cmUiKwoaVGVzdGluZ1NpbXBsZVZhbHVlUHJvdG9idWYSDQoF",
            "VmFsdWUYASABKAkiUAobVGVzdGluZ1NpbXBsZVN0cnVjdFByb3RvYnVmEjEK",
            "D1NpbXBsZVN0cnVjdHVyZRgBIAEoCzIYLlRlc3RpbmcuU2ltcGxlU3RydWN0",
            "dXJlIlMKHFRlc3RpbmdDb21wbGV4U3RydWN0UHJvdG9idWYSMwoQQ29tcGxl",
            "eFN0cnVjdHVyZRgBIAEoCzIZLlRlc3RpbmcuQ29tcGxleFN0cnVjdHVyZUIQ",
            "SANaDHBhY2thZ2U7bWFpbmIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Testing.SimpleStructure), global::Testing.SimpleStructure.Parser, new[]{ "Value1", "Value2", "Value3", "Value4" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Testing.ComplexStructure), global::Testing.ComplexStructure.Parser, new[]{ "Value", "Next" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Testing.TestingSimpleValueProtobuf), global::Testing.TestingSimpleValueProtobuf.Parser, new[]{ "Value" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Testing.TestingSimpleStructProtobuf), global::Testing.TestingSimpleStructProtobuf.Parser, new[]{ "SimpleStructure" }, null, null, null, null),
            new pbr::GeneratedClrTypeInfo(typeof(global::Testing.TestingComplexStructProtobuf), global::Testing.TestingComplexStructProtobuf.Parser, new[]{ "ComplexStructure" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  public sealed partial class SimpleStructure : pb::IMessage<SimpleStructure>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<SimpleStructure> _parser = new pb::MessageParser<SimpleStructure>(() => new SimpleStructure());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<SimpleStructure> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Testing.TestingProtobufReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public SimpleStructure() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public SimpleStructure(SimpleStructure other) : this() {
      value1_ = other.value1_;
      value2_ = other.value2_;
      value3_ = other.value3_;
      value4_ = other.value4_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public SimpleStructure Clone() {
      return new SimpleStructure(this);
    }

    /// <summary>Field number for the "Value1" field.</summary>
    public const int Value1FieldNumber = 1;
    private int value1_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int Value1 {
      get { return value1_; }
      set {
        value1_ = value;
      }
    }

    /// <summary>Field number for the "Value2" field.</summary>
    public const int Value2FieldNumber = 2;
    private long value2_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long Value2 {
      get { return value2_; }
      set {
        value2_ = value;
      }
    }

    /// <summary>Field number for the "Value3" field.</summary>
    public const int Value3FieldNumber = 3;
    private float value3_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public float Value3 {
      get { return value3_; }
      set {
        value3_ = value;
      }
    }

    /// <summary>Field number for the "Value4" field.</summary>
    public const int Value4FieldNumber = 4;
    private string value4_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Value4 {
      get { return value4_; }
      set {
        value4_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as SimpleStructure);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(SimpleStructure other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Value1 != other.Value1) return false;
      if (Value2 != other.Value2) return false;
      if (!pbc::ProtobufEqualityComparers.BitwiseSingleEqualityComparer.Equals(Value3, other.Value3)) return false;
      if (Value4 != other.Value4) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Value1 != 0) hash ^= Value1.GetHashCode();
      if (Value2 != 0L) hash ^= Value2.GetHashCode();
      if (Value3 != 0F) hash ^= pbc::ProtobufEqualityComparers.BitwiseSingleEqualityComparer.GetHashCode(Value3);
      if (Value4.Length != 0) hash ^= Value4.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (Value1 != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(Value1);
      }
      if (Value2 != 0L) {
        output.WriteRawTag(16);
        output.WriteInt64(Value2);
      }
      if (Value3 != 0F) {
        output.WriteRawTag(29);
        output.WriteFloat(Value3);
      }
      if (Value4.Length != 0) {
        output.WriteRawTag(34);
        output.WriteString(Value4);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (Value1 != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(Value1);
      }
      if (Value2 != 0L) {
        output.WriteRawTag(16);
        output.WriteInt64(Value2);
      }
      if (Value3 != 0F) {
        output.WriteRawTag(29);
        output.WriteFloat(Value3);
      }
      if (Value4.Length != 0) {
        output.WriteRawTag(34);
        output.WriteString(Value4);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Value1 != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(Value1);
      }
      if (Value2 != 0L) {
        size += 1 + pb::CodedOutputStream.ComputeInt64Size(Value2);
      }
      if (Value3 != 0F) {
        size += 1 + 4;
      }
      if (Value4.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Value4);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(SimpleStructure other) {
      if (other == null) {
        return;
      }
      if (other.Value1 != 0) {
        Value1 = other.Value1;
      }
      if (other.Value2 != 0L) {
        Value2 = other.Value2;
      }
      if (other.Value3 != 0F) {
        Value3 = other.Value3;
      }
      if (other.Value4.Length != 0) {
        Value4 = other.Value4;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            Value1 = input.ReadInt32();
            break;
          }
          case 16: {
            Value2 = input.ReadInt64();
            break;
          }
          case 29: {
            Value3 = input.ReadFloat();
            break;
          }
          case 34: {
            Value4 = input.ReadString();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 8: {
            Value1 = input.ReadInt32();
            break;
          }
          case 16: {
            Value2 = input.ReadInt64();
            break;
          }
          case 29: {
            Value3 = input.ReadFloat();
            break;
          }
          case 34: {
            Value4 = input.ReadString();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class ComplexStructure : pb::IMessage<ComplexStructure>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<ComplexStructure> _parser = new pb::MessageParser<ComplexStructure>(() => new ComplexStructure());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ComplexStructure> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Testing.TestingProtobufReflection.Descriptor.MessageTypes[1]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ComplexStructure() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ComplexStructure(ComplexStructure other) : this() {
      value_ = other.value_;
      next_ = other.next_ != null ? other.next_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ComplexStructure Clone() {
      return new ComplexStructure(this);
    }

    /// <summary>Field number for the "Value" field.</summary>
    public const int ValueFieldNumber = 1;
    private int value_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int Value {
      get { return value_; }
      set {
        value_ = value;
      }
    }

    /// <summary>Field number for the "Next" field.</summary>
    public const int NextFieldNumber = 2;
    private global::Testing.ComplexStructure next_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Testing.ComplexStructure Next {
      get { return next_; }
      set {
        next_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ComplexStructure);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ComplexStructure other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Value != other.Value) return false;
      if (!object.Equals(Next, other.Next)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Value != 0) hash ^= Value.GetHashCode();
      if (next_ != null) hash ^= Next.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (Value != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(Value);
      }
      if (next_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Next);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (Value != 0) {
        output.WriteRawTag(8);
        output.WriteInt32(Value);
      }
      if (next_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(Next);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Value != 0) {
        size += 1 + pb::CodedOutputStream.ComputeInt32Size(Value);
      }
      if (next_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(Next);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ComplexStructure other) {
      if (other == null) {
        return;
      }
      if (other.Value != 0) {
        Value = other.Value;
      }
      if (other.next_ != null) {
        if (next_ == null) {
          Next = new global::Testing.ComplexStructure();
        }
        Next.MergeFrom(other.Next);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 8: {
            Value = input.ReadInt32();
            break;
          }
          case 18: {
            if (next_ == null) {
              Next = new global::Testing.ComplexStructure();
            }
            input.ReadMessage(Next);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 8: {
            Value = input.ReadInt32();
            break;
          }
          case 18: {
            if (next_ == null) {
              Next = new global::Testing.ComplexStructure();
            }
            input.ReadMessage(Next);
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class TestingSimpleValueProtobuf : pb::IMessage<TestingSimpleValueProtobuf>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<TestingSimpleValueProtobuf> _parser = new pb::MessageParser<TestingSimpleValueProtobuf>(() => new TestingSimpleValueProtobuf());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<TestingSimpleValueProtobuf> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Testing.TestingProtobufReflection.Descriptor.MessageTypes[2]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingSimpleValueProtobuf() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingSimpleValueProtobuf(TestingSimpleValueProtobuf other) : this() {
      value_ = other.value_;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingSimpleValueProtobuf Clone() {
      return new TestingSimpleValueProtobuf(this);
    }

    /// <summary>Field number for the "Value" field.</summary>
    public const int ValueFieldNumber = 1;
    private string value_ = "";
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Value {
      get { return value_; }
      set {
        value_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as TestingSimpleValueProtobuf);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(TestingSimpleValueProtobuf other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Value != other.Value) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Value.Length != 0) hash ^= Value.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (Value.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Value);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (Value.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Value);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Value.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Value);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(TestingSimpleValueProtobuf other) {
      if (other == null) {
        return;
      }
      if (other.Value.Length != 0) {
        Value = other.Value;
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            Value = input.ReadString();
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            Value = input.ReadString();
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class TestingSimpleStructProtobuf : pb::IMessage<TestingSimpleStructProtobuf>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<TestingSimpleStructProtobuf> _parser = new pb::MessageParser<TestingSimpleStructProtobuf>(() => new TestingSimpleStructProtobuf());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<TestingSimpleStructProtobuf> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Testing.TestingProtobufReflection.Descriptor.MessageTypes[3]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingSimpleStructProtobuf() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingSimpleStructProtobuf(TestingSimpleStructProtobuf other) : this() {
      simpleStructure_ = other.simpleStructure_ != null ? other.simpleStructure_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingSimpleStructProtobuf Clone() {
      return new TestingSimpleStructProtobuf(this);
    }

    /// <summary>Field number for the "SimpleStructure" field.</summary>
    public const int SimpleStructureFieldNumber = 1;
    private global::Testing.SimpleStructure simpleStructure_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Testing.SimpleStructure SimpleStructure {
      get { return simpleStructure_; }
      set {
        simpleStructure_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as TestingSimpleStructProtobuf);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(TestingSimpleStructProtobuf other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(SimpleStructure, other.SimpleStructure)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (simpleStructure_ != null) hash ^= SimpleStructure.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (simpleStructure_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(SimpleStructure);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (simpleStructure_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(SimpleStructure);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (simpleStructure_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(SimpleStructure);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(TestingSimpleStructProtobuf other) {
      if (other == null) {
        return;
      }
      if (other.simpleStructure_ != null) {
        if (simpleStructure_ == null) {
          SimpleStructure = new global::Testing.SimpleStructure();
        }
        SimpleStructure.MergeFrom(other.SimpleStructure);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (simpleStructure_ == null) {
              SimpleStructure = new global::Testing.SimpleStructure();
            }
            input.ReadMessage(SimpleStructure);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            if (simpleStructure_ == null) {
              SimpleStructure = new global::Testing.SimpleStructure();
            }
            input.ReadMessage(SimpleStructure);
            break;
          }
        }
      }
    }
    #endif

  }

  public sealed partial class TestingComplexStructProtobuf : pb::IMessage<TestingComplexStructProtobuf>
  #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
  #endif
  {
    private static readonly pb::MessageParser<TestingComplexStructProtobuf> _parser = new pb::MessageParser<TestingComplexStructProtobuf>(() => new TestingComplexStructProtobuf());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<TestingComplexStructProtobuf> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Testing.TestingProtobufReflection.Descriptor.MessageTypes[4]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingComplexStructProtobuf() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingComplexStructProtobuf(TestingComplexStructProtobuf other) : this() {
      complexStructure_ = other.complexStructure_ != null ? other.complexStructure_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public TestingComplexStructProtobuf Clone() {
      return new TestingComplexStructProtobuf(this);
    }

    /// <summary>Field number for the "ComplexStructure" field.</summary>
    public const int ComplexStructureFieldNumber = 1;
    private global::Testing.ComplexStructure complexStructure_;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Testing.ComplexStructure ComplexStructure {
      get { return complexStructure_; }
      set {
        complexStructure_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as TestingComplexStructProtobuf);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(TestingComplexStructProtobuf other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(ComplexStructure, other.ComplexStructure)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (complexStructure_ != null) hash ^= ComplexStructure.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
    #else
      if (complexStructure_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(ComplexStructure);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (complexStructure_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(ComplexStructure);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
    #endif

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (complexStructure_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(ComplexStructure);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(TestingComplexStructProtobuf other) {
      if (other == null) {
        return;
      }
      if (other.complexStructure_ != null) {
        if (complexStructure_ == null) {
          ComplexStructure = new global::Testing.ComplexStructure();
        }
        ComplexStructure.MergeFrom(other.ComplexStructure);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
    #else
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 10: {
            if (complexStructure_ == null) {
              ComplexStructure = new global::Testing.ComplexStructure();
            }
            input.ReadMessage(ComplexStructure);
            break;
          }
        }
      }
    #endif
    }

    #if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            if (complexStructure_ == null) {
              ComplexStructure = new global::Testing.ComplexStructure();
            }
            input.ReadMessage(ComplexStructure);
            break;
          }
        }
      }
    }
    #endif

  }

  #endregion

}

#endregion Designer generated code
