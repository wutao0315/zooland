using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweeper.Core.Tag
{

    public sealed class Tags
    {
        private Tags()
        {
        }

        /**
         * A constant for setting the span kind to indicate that it represents a server span.
         */
        public static readonly string SPAN_KIND_SERVER = "server";

    /**
     * A constant for setting the span kind to indicate that it represents a client span.
     */
    public static readonly string SPAN_KIND_CLIENT = "client";

    /**
     * A constant for setting the span kind to indicate that it represents a producer span, in a messaging scenario.
     */
    public static readonly string SPAN_KIND_PRODUCER = "producer";

    /**
     * A constant for setting the span kind to indicate that it represents a consumer span, in a messaging scenario.
     */
    public static readonly string SPAN_KIND_CONSUMER = "consumer";

    /**
     * The service name for a span, which overrides any default "service name" property defined
     * in a tracer's config. This tag is meant to only be used when a tracer is reporting spans
     * on behalf of another service (for example, a service mesh reporting on behalf of the services
     * it is proxying). This tag does not need to be used when reporting spans for the service the
     * tracer is running in.
     *
     * @see #PEER_SERVICE
     */
    public static readonly StringTag SERVICE = new StringTag("service");

        /**
         * HTTP_URL records the url of the incoming request.
         */
        public static readonly StringTag HTTP_URL = new StringTag("http.url");

        /**
         * HTTP_STATUS records the http status code of the response.
         */
        public static readonly IntTag HTTP_STATUS = new IntTag("http.status_code");

        /**
         * HTTP_METHOD records the http method. Case-insensitive.
         */
        public static readonly StringTag HTTP_METHOD = new StringTag("http.method");

        /**
         * PEER_HOST_IPV4 records IPv4 host address of the peer.
         */
        public static readonly IntOrStringTag PEER_HOST_IPV4 = new IntOrStringTag("peer.ipv4");

        /**
         * PEER_HOST_IPV6 records the IPv6 host address of the peer.
         */
        public static readonly StringTag PEER_HOST_IPV6 = new StringTag("peer.ipv6");

        /**
         * PEER_SERVICE records the service name of the peer service.
         *
         * @see #SERVICE
         */
        public static readonly StringTag PEER_SERVICE = new StringTag("peer.service");

        /**
         * PEER_HOSTNAME records the host name of the peer.
         */
        public static readonly StringTag PEER_HOSTNAME = new StringTag("peer.hostname");

        /**
         * PEER_PORT records the port number of the peer.
         */
        public static readonly IntTag PEER_PORT = new IntTag("peer.port");

        /**
         * SAMPLING_PRIORITY determines the priority of sampling this Span.
         */
        public static readonly IntTag SAMPLING_PRIORITY = new IntTag("sampling.priority");

        /**
         * SPAN_KIND hints at the relationship between spans, e.g. client/server.
         */
        public static readonly StringTag SPAN_KIND = new StringTag("span.kind");

        /**
         * COMPONENT is a low-cardinality identifier of the module, library, or package that is instrumented.
         */
        public static readonly StringTag COMPONENT = new StringTag("component");

        /**
         * ERROR indicates whether a Span ended in an error state.
         */
        public static readonly BooleanTag ERROR = new BooleanTag("error");

        /**
         * DB_TYPE indicates the type of Database.
         * For any SQL database, "sql". For others, the lower-case database category, e.g. "cassandra", "hbase", or "redis"
         */
        public static readonly StringTag DB_TYPE = new StringTag("db.type");

        /**
         * DB_INSTANCE indicates the instance name of Database.
         * If the jdbc.url="jdbc:mysql://127.0.0.1:3306/customers", instance name is "customers".
         */
        public static readonly StringTag DB_INSTANCE = new StringTag("db.instance");

        /**
         * DB_USER indicates the user name of Database, e.g. "readonly_user" or "reporting_user"
         */
        public static readonly StringTag DB_USER = new StringTag("db.user");

        /**
         * DB_STATEMENT records a database statement for the given database type.
         * For db.type="SQL", "SELECT * FROM wuser_table". For db.type="redis", "SET mykey "WuValue".
         */
        public static readonly StringTag DB_STATEMENT = new StringTag("db.statement");

        /**
         * MESSAGE_BUS_DESTINATION records an address at which messages can be exchanged.
         * E.g. A Kafka record has an associated "topic name" that can be extracted by the instrumented
         * producer or consumer and stored using this tag.
         */
        public static readonly StringTag MESSAGE_BUS_DESTINATION = new StringTag("message_bus.destination");
    }
}

