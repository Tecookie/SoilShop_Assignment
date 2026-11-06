workspace "Potting Soil Shop" "Internal shop: explainable advice, atomic stock, replenishment and count approval. C1/C2 only." {
    !impliedRelationships false
    model {
        admin = person "Admin / Owner" "Accounts, metadata/prices, cancellations, stock-count approvals and backup requests."
        employee = person "Employee" "Receives goods, advises customers, records sales/counts and acts on replenishment."
        supplier = person "Supplier" "Supplies pre-packed bags and label information offline; no account." {
            tags "External"
        }
        customer = person "Walk-in Customer" "Describes needs and buys at the counter through staff; no account." {
            tags "External"
        }
        shop = softwareSystem "Potting Soil Shop" "Internal catalogue, advice, stock documents, ledger, replenishment, stocktaking and background jobs." {
            web = container "React Web" "Plain CSS forms; advice reasons/source, receipts/sales, counts, suggestions, reports and Admin pages." "React / Vite / TypeScript / Nginx"
            api = container "REST API" "JWT roles, layered Services/Repositories, AutoMapper DTOs, atomic stock/ledger/outbox; hosted outbox relay." "ASP.NET Core 9 / EF Core 9 / Npgsql"
            rpc = container "Advisory and Calculation" "Stateless CalculateTotals and RecommendProducts; known metadata, positive-match ranking, explanations/rule version." ".NET gRPC / Protobuf / HTTP2"
            worker = container "Background Worker" "Redis consumer; low-stock rechecks, suggestion lifecycle, summaries, serial pg_dump and retention; retries/reconciliation." ".NET Worker / BackgroundService"
            pg = container "Shop Database" "Catalogue/tags, SKU stock/version, documents/prices, count approvals, ledger, suggestions, outbox and job state." "PostgreSQL 16" {
                tags "Database"
            }
            redis = container "Event Broker" "Consumer-group stream delivery with pending ACK; AOF and persistent volume." "Redis Streams"
            backups = container "Backup Archive Volume" "Worker-written .dump archives; retain seven successful Daily/Manual files total." "Docker named volume / pg_dump custom format" {
                tags "FileStore"
            }
        }
        supplier -> employee "Supplies goods and source product information" "Offline delivery / documents"
        customer -> employee "Describes needs and buys products" "Counter interaction"
        employee -> shop "Receives, advises, sells, counts and replenishes" "Private web application"
        admin -> shop "Maintains catalogue/prices/accounts and approves corrections" "Private web application"
        employee -> web "Uses operating screens and conflict feedback" "HTTP, local demo"
        admin -> web "Uses administrative and operating screens" "HTTP, local demo"
        web -> api "Submits authenticated commands/queries and polls job results" "REST / JSON / JWT"
        api -> rpc "Requests totals and explained product matches; receives result" "gRPC / HTTP2"
        api -> pg "Reads trusted data and commits documents, stock, ledger and outbox" "EF Core / Npgsql / SQL transactions"
        api -> redis "Publishes committed outbox intent with hosted relay" "XADD"
        worker -> redis "Consumes and acknowledges work; reclaims pending delivery" "XREADGROUP / XACK / XAUTOCLAIM"
        worker -> pg "Reads fresh stock and persists suggestions/jobs/reports; exports database" "EF Core / Npgsql / pg_dump connection"
        worker -> backups "Writes/verifies archives and removes files outside retention" "Filesystem I/O"
    }
    views {
        systemContext shop "Context" {
            include admin employee supplier customer shop
            autoLayout lr
        }
        container shop "Containers" {
            include admin employee web api rpc worker pg redis backups
            autoLayout lr
        }
        container shop "Synchronous" {
            include employee admin web api rpc pg
            autoLayout lr
        }
        container shop "Asynchronous" {
            include api worker pg redis backups
            autoLayout lr
        }
        styles {
            element "Element" {
                background #FFFFFF
                color #142B49
                stroke #48617C
            }
            element "Person" {
                shape Person
            }
            element "Software System" {
                background #E8F6FA
                stroke #007C91
            }
            element "Database" {
                shape Cylinder
            }
            element "FileStore" {
                shape Folder
            }
            element "External" {
                background #F1F5F9
            }
            relationship "Relationship" {
                color #48617C
            }
        }
    }
}
