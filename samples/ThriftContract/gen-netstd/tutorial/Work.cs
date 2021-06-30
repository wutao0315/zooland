/**
 * Autogenerated by Thrift Compiler (0.14.2)
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 *  @generated
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Thrift;
using Thrift.Collections;

using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Protocol.Utilities;
using Thrift.Transport;
using Thrift.Transport.Client;
using Thrift.Transport.Server;
using Thrift.Processor;


#pragma warning disable IDE0079  // remove unnecessary pragmas
#pragma warning disable IDE1006  // parts of the code use IDL spelling

namespace tutorial
{

  /// <summary>
  /// Structs are the basic complex data structures. They are comprised of fields
  /// which each have an integer identifier, a type, a symbolic name, and an
  /// optional default value.
  /// 
  /// Fields can be declared "optional", which ensures they will not be included
  /// in the serialized output if they aren't set.  Note that this requires some
  /// manual management in some languages.
  /// </summary>
  public partial class Work : TBase
  {
    private int _num1;
    private int _num2;
    private global::tutorial.Operation _op;
    private string _comment;

    public int Num1
    {
      get
      {
        return _num1;
      }
      set
      {
        __isset.num1 = true;
        this._num1 = value;
      }
    }

    public int Num2
    {
      get
      {
        return _num2;
      }
      set
      {
        __isset.num2 = true;
        this._num2 = value;
      }
    }

    /// <summary>
    /// 
    /// <seealso cref="global::tutorial.Operation"/>
    /// </summary>
    public global::tutorial.Operation Op
    {
      get
      {
        return _op;
      }
      set
      {
        __isset.op = true;
        this._op = value;
      }
    }

    public string Comment
    {
      get
      {
        return _comment;
      }
      set
      {
        __isset.comment = true;
        this._comment = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool num1;
      public bool num2;
      public bool op;
      public bool comment;
    }

    public Work()
    {
      this._num1 = 0;
      this.__isset.num1 = true;
    }

    public Work DeepCopy()
    {
      var tmp0 = new Work();
      if(__isset.num1)
      {
        tmp0.Num1 = this.Num1;
      }
      tmp0.__isset.num1 = this.__isset.num1;
      if(__isset.num2)
      {
        tmp0.Num2 = this.Num2;
      }
      tmp0.__isset.num2 = this.__isset.num2;
      if(__isset.op)
      {
        tmp0.Op = this.Op;
      }
      tmp0.__isset.op = this.__isset.op;
      if((Comment != null) && __isset.comment)
      {
        tmp0.Comment = this.Comment;
      }
      tmp0.__isset.comment = this.__isset.comment;
      return tmp0;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        TField field;
        await iprot.ReadStructBeginAsync(cancellationToken);
        while (true)
        {
          field = await iprot.ReadFieldBeginAsync(cancellationToken);
          if (field.Type == TType.Stop)
          {
            break;
          }

          switch (field.ID)
          {
            case 1:
              if (field.Type == TType.I32)
              {
                Num1 = await iprot.ReadI32Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.I32)
              {
                Num2 = await iprot.ReadI32Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 3:
              if (field.Type == TType.I32)
              {
                Op = (global::tutorial.Operation)await iprot.ReadI32Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 4:
              if (field.Type == TType.String)
              {
                Comment = await iprot.ReadStringAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            default: 
              await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              break;
          }

          await iprot.ReadFieldEndAsync(cancellationToken);
        }

        await iprot.ReadStructEndAsync(cancellationToken);
      }
      finally
      {
        iprot.DecrementRecursionDepth();
      }
    }

    public async global::System.Threading.Tasks.Task WriteAsync(TProtocol oprot, CancellationToken cancellationToken)
    {
      oprot.IncrementRecursionDepth();
      try
      {
        var struc = new TStruct("Work");
        await oprot.WriteStructBeginAsync(struc, cancellationToken);
        var field = new TField();
        if(__isset.num1)
        {
          field.Name = "num1";
          field.Type = TType.I32;
          field.ID = 1;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteI32Async(Num1, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.num2)
        {
          field.Name = "num2";
          field.Type = TType.I32;
          field.ID = 2;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteI32Async(Num2, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.op)
        {
          field.Name = "op";
          field.Type = TType.I32;
          field.ID = 3;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteI32Async((int)Op, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Comment != null) && __isset.comment)
        {
          field.Name = "comment";
          field.Type = TType.String;
          field.ID = 4;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteStringAsync(Comment, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        await oprot.WriteFieldStopAsync(cancellationToken);
        await oprot.WriteStructEndAsync(cancellationToken);
      }
      finally
      {
        oprot.DecrementRecursionDepth();
      }
    }

    public override bool Equals(object that)
    {
      if (!(that is Work other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.num1 == other.__isset.num1) && ((!__isset.num1) || (System.Object.Equals(Num1, other.Num1))))
        && ((__isset.num2 == other.__isset.num2) && ((!__isset.num2) || (System.Object.Equals(Num2, other.Num2))))
        && ((__isset.op == other.__isset.op) && ((!__isset.op) || (System.Object.Equals(Op, other.Op))))
        && ((__isset.comment == other.__isset.comment) && ((!__isset.comment) || (System.Object.Equals(Comment, other.Comment))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if(__isset.num1)
        {
          hashcode = (hashcode * 397) + Num1.GetHashCode();
        }
        if(__isset.num2)
        {
          hashcode = (hashcode * 397) + Num2.GetHashCode();
        }
        if(__isset.op)
        {
          hashcode = (hashcode * 397) + Op.GetHashCode();
        }
        if((Comment != null) && __isset.comment)
        {
          hashcode = (hashcode * 397) + Comment.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var sb = new StringBuilder("Work(");
      int tmp1 = 0;
      if(__isset.num1)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Num1: ");
        Num1.ToString(sb);
      }
      if(__isset.num2)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Num2: ");
        Num2.ToString(sb);
      }
      if(__isset.op)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Op: ");
        Op.ToString(sb);
      }
      if((Comment != null) && __isset.comment)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Comment: ");
        Comment.ToString(sb);
      }
      sb.Append(')');
      return sb.ToString();
    }
  }

}