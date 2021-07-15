﻿using System.Linq;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence.Dtos;

namespace Umbraco.Core.Migrations.Upgrade.V_8_16_0
{
    public class AddPropertyTypeGroupColumns : MigrationBase
    {
        public AddPropertyTypeGroupColumns(IMigrationContext context)
            : base(context)
        { }

        public override void Migrate()
        {
            AddColumn<PropertyTypeGroupDto>("type");

            AddColumn<PropertyTypeGroupDto>("alias", out var sqls);

            var dtos = Database.Fetch<PropertyTypeGroupDto>();
            foreach (var dtosPerAlias in dtos.GroupBy(x => x.Text.ToSafeAlias(true)))
            {
                var dtosPerAliasAndText = dtosPerAlias.GroupBy(x => x.Text);
                var numberSuffix = 1;
                foreach (var dtosPerText in dtosPerAliasAndText)
                {
                    foreach (var dto in dtosPerText)
                    {
                        dto.Alias = dtosPerAlias.Key;

                        if (numberSuffix > 1)
                        {
                            // More than 1 name found for the alias, so add a suffix
                            dto.Alias += numberSuffix;
                        }

                        Database.Update(dto, x => new { x.Alias });
                    }

                    numberSuffix++;
                }

                if (numberSuffix > 2)
                {
                    Logger.Error<AddPropertyTypeGroupColumns>("Detected the same alias {Alias} for different property group names {Names}, the migration added suffixes, but this might break backwards compatibility.", dtosPerAlias.Key, dtosPerAliasAndText.Select(x => x.Key));
                }
            }

            foreach (var sql in sqls)
                Database.Execute(sql);
        }
    }
}
