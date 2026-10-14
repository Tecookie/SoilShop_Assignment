workspace "Broker Property Catalogue" "PRN232 internal broker catalogue; C4 Level 1 and Level 2 only" {
    !impliedRelationships false
    model {
        admin = person "Admin" "Manages staff, catalogue and backups."
        employee = person "Employee" "Maintains allocated inventory, prices and management status."
        developer = person "Property Developer" "Supplies inventory and incomplete details offline." {
            tags "External"
        }
        catalogue = softwareSystem "Broker Property Catalogue" "Internal catalogue for one broker organization." {
            web = container "React Web" "Forms, catalogue, previews, progress and summaries." "React / TypeScript / Nginx"
            api = container "REST API" "JWT, CRUD, validation and durable operation requests." "ASP.NET Core 10 / EF Core 10"
            numbering = container "Numbering Service" "Stateless deterministic code generation." ".NET gRPC / Protobuf / HTTP/2"
            worker = container "Background Worker" "Outbox relay, consumers, batch creation, summaries and backups." ".NET Worker / BackgroundService"
            sql = container "Catalogue Database" "Business records and durable operation state." "SQL Server 2022" {
                tags "Database"
            }
            redis = container "Message Broker" "Consumer-group delivery of async requests." "Redis Streams" {
                tags "Queue"
            }
            backups = container "Backup Files" "Full .bak files; 7 daily and 7 change/manual retained." "Docker named volume" {
                tags "Database"
            }
        }
        developer -> employee "Supplies inventory and allocation updates" "Offline communication"
        admin -> catalogue "Manages staff, inventory and backup requests" "Web browser"
        employee -> catalogue "Maintains catalogue and views progress" "Web browser"
        admin -> web "Uses administration and catalogue screens" "HTTP, local demo"
        employee -> web "Uses catalogue screens" "HTTP, local demo"
        web -> api "Submits commands and queries; polls progress" "REST/JSON via /api proxy"
        api -> numbering "Generates unit-code previews" "gRPC / HTTP/2"
        api -> sql "Reads and commits catalogue plus outbox intent" "EF Core / TDS"
        worker -> sql "Reads outbox and commits jobs; commands backups" "EF Core / TDS"
        worker -> redis "Publishes requests and consumes/acknowledges jobs" "XADD / XREADGROUP / XACK"
        sql -> backups "Writes full database backups" "BACKUP DATABASE / disk I/O"
        worker -> backups "Deletes files outside retention limits" "Filesystem I/O"
    }
    views {
        systemContext catalogue "Context" {
            include admin employee developer catalogue
            autoLayout lr
        }
        container catalogue "Containers" {
            include admin employee web api numbering worker sql redis backups
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
            element "External" {
                background #F1F5F9
            }
            relationship "Relationship" {
                color #48617C
            }
        }
    }
}
