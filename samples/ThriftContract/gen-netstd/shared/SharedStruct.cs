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

namespace shared
{

  public partial class SharedStruct : TBase
  {
    private int _key;
    private string _value;

    public int Key
    {
      get
      {
        return _key;
      }
      set
      {
        __isset.key = true;
        this._key = value;
      }
    }

    public string Value
    {
      get
      {
        return _value;
      }
      set
      {
        __isset.@value = true;
        this._value = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool key;
      public bool @value;
    }

    public SharedStruct()
    {
    }

    public SharedStruct DeepCopy()
    {
      var tmp0 = new SharedStruct();
      if(__isset.key)
      {
        tmp0.Key = this.Key;
      }
      tmp0.__isset.key = this.__isset.key;
      if((Value != null) && __isset.@value)
      {
        tmp0.Value = this.Value;
      }
      tmp0.__isset.@value = this.__isset.@value;
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
                Key = await iprot.ReadI32Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.String)
              {
                Value = await iprot.ReadStringAsync(cancellationToken);
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
        var struc = new TStruct("SharedStruct");
        await oprot.WriteStructBeginAsync(struc, cancellationToken);
        var field = new TField();
        if(__isset.key)
        {
          field.Name = "key";
          field.Type = TType.I32;
          field.ID = 1;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteI32Async(Key, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Value != null) && __isset.@value)
        {
          field.Name = "value";
          field.Type = TType.String;
          field.ID = 2;
          await oprot.WriteFieldBeginAsync(field, cancellationToken);
          await oprot.WriteStringAsync(Value, cancellationToken);
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
      if (!(that is SharedStruct other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.key == other.__isset.key) && ((!__isset.key) || (System.Object.Equals(Key, other.Key))))
        && ((__isset.@value == other.__isset.@value) && ((!__isset.@value) || (System.Object.Equals(Value, other.Value))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if(__isset.key)
        {
          hashcode = (hashcode * 397) + Key.GetHashCode();
        }
        if((Value != null) && __isset.@value)
        {
          hashcode = (hashcode * 397) + Value.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var sb = new StringBuilder("SharedStruct(");
      int tmp1 = 0;
      if(__isset.key)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Key: ");
        Key.ToString(sb);
      }
      if((Value != null) && __isset.@value)
      {
        if(0 < tmp1++) { sb.Append(", "); }
        sb.Append("Value: ");
        Value.ToString(sb);
      }
      sb.Append(')');
      return sb.ToString();
    }
  }

}