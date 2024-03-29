namespace Rappen.XRM.Helpers.FetchXML
{
    public enum @operator
    {
        /// <remarks/>
        eq,

        /// <remarks/>
        neq,

        /// <remarks/>
        ne,

        /// <remarks/>
        gt,

        /// <remarks/>
        ge,

        /// <remarks/>
        le,

        /// <remarks/>
        lt,

        /// <remarks/>
        like,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-like")]
        notlike,

        /// <remarks/>
        @in,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-in")]
        notin,

        /// <remarks/>
        between,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-between")]
        notbetween,

        /// <remarks/>
        @null,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-null")]
        notnull,

        /// <remarks/>
        yesterday,

        /// <remarks/>
        today,

        /// <remarks/>
        tomorrow,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-seven-days")]
        lastsevendays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-seven-days")]
        nextsevendays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-week")]
        lastweek,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-week")]
        thisweek,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-week")]
        nextweek,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-month")]
        lastmonth,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-month")]
        thismonth,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-month")]
        nextmonth,

        /// <remarks/>
        on,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("on-or-before")]
        onorbefore,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("on-or-after")]
        onorafter,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-year")]
        lastyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-year")]
        thisyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-year")]
        nextyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-hours")]
        lastxhours,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-hours")]
        nextxhours,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-days")]
        lastxdays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-days")]
        nextxdays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-weeks")]
        lastxweeks,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-weeks")]
        nextxweeks,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-months")]
        lastxmonths,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-months")]
        nextxmonths,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-months")]
        olderthanxmonths,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-years")]
        olderthanxyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-weeks")]
        olderthanxweeks,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-days")]
        olderthanxdays,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-hours")]
        olderthanxhours,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("olderthan-x-minutes")]
        olderthanxminutes,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-years")]
        lastxyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-years")]
        nextxyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-userid")]
        equserid,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("ne-userid")]
        neuserid,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-userteams")]
        equserteams,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-useroruserteams")]
        equseroruserteams,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-useroruserhierarchy")]
        equseroruserhierarchy,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-useroruserhierarchyandteams")]
        equseroruserhierarchyandteams,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-businessid")]
        eqbusinessid,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("ne-businessid")]
        nebusinessid,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-userlanguage")]
        equserlanguage,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-fiscal-year")]
        thisfiscalyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("this-fiscal-period")]
        thisfiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-fiscal-year")]
        nextfiscalyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-fiscal-period")]
        nextfiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-fiscal-year")]
        lastfiscalyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-fiscal-period")]
        lastfiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-fiscal-years")]
        lastxfiscalyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("last-x-fiscal-periods")]
        lastxfiscalperiods,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-fiscal-years")]
        nextxfiscalyears,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("next-x-fiscal-periods")]
        nextxfiscalperiods,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-fiscal-year")]
        infiscalyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-fiscal-period")]
        infiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-fiscal-period-and-year")]
        infiscalperiodandyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-or-before-fiscal-period-and-year")]
        inorbeforefiscalperiodandyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("in-or-after-fiscal-period-and-year")]
        inorafterfiscalperiodandyear,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("begins-with")]
        beginswith,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-begin-with")]
        notbeginwith,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("ends-with")]
        endswith,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-end-with")]
        notendwith,

        /// <remarks/>
        under,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-or-under")]
        eqorunder,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-under")]
        notunder,

        /// <remarks/>
        above,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("eq-or-above")]
        eqorabove,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("contain-values")]
        containvalues,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("not-contain-values")]
        notcontainvalues,
    }

    public enum AggregateType
    {
        /// <remarks/>
        count,

        /// <remarks/>
        countcolumn,

        /// <remarks/>
        sum,

        /// <remarks/>
        avg,

        /// <remarks/>
        min,

        /// <remarks/>
        max,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    public enum DateGroupingType
    {
        /// <remarks/>
        day,

        /// <remarks/>
        week,

        /// <remarks/>
        month,

        /// <remarks/>
        quarter,

        /// <remarks/>
        year,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("fiscal-period")]
        fiscalperiod,

        /// <remarks/>
        [System.Xml.Serialization.XmlEnumAttribute("fiscal-year")]
        fiscalyear,
    }

    public enum LinkType
    {
        inner,
        outer,
        any,
        [System.Xml.Serialization.XmlEnumAttribute("not any")]
        notany,
        all,
        [System.Xml.Serialization.XmlEnumAttribute("not all")]
        notall,
        exists,
        [System.Xml.Serialization.XmlEnumAttribute("in")]
        in_,
        matchfirstrowusingcrossapply
    }
}