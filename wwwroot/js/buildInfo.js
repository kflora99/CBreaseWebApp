window.breaseBuildInfo = {
    formatBuildTimestamp: function (utcTimestamp) {
        try {
            if (!utcTimestamp) {
                return "unknown";
            }

            const date = new Date(utcTimestamp);
            if (Number.isNaN(date.getTime())) {
                return utcTimestamp;
            }

            if (!window.Intl || !Intl.DateTimeFormat) {
                return `${date.toLocaleString()} local`;
            }

            const parts = new Intl.DateTimeFormat("en-US", {
                month: "short",
                day: "numeric",
                year: "numeric",
                hour: "numeric",
                minute: "2-digit"
            }).formatToParts(date);

            const value = function (type) {
                const part = parts.find(function (candidate) {
                    return candidate.type === type;
                });

                return part ? part.value : "";
            };

            const dateText = `${value("month")} ${value("day")}, ${value("year")}`;
            const timeText = `${value("hour")}:${value("minute")} ${value("dayPeriod")}`.trim();
            return `${dateText}\n${timeText}`;
        } catch (e) {
            return utcTimestamp || "unknown";
        }
    }
};
