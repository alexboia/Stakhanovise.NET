MAP=sk_mapping.dbmap
DEFINITIONS=*.dbdef
OUTPUT=console(func=true; seq=true; tbl=true; tbl_index=true; tbl_unq=true)
OUTPUT=sql_script(proj=LVD.Stakhanovise.NET; dir=Db/scripts; mode=consolidated; file=sk_db.sql; item_group=SK_DbScripts; build_action=None)
OUTPUT=sql_script(proj=LVD.Stakhanovise.NET; dir=Setup/BuiltInDbAssetsSetup/Scripts; mode=single; item_group=SK_Setup_DbScripts; file=$db_object$.sql; build_action=EmbeddedResource; copy_output=Never)
OUTPUT=db_create(connection_string=host:localhost,port:5432,user:postgres,password:postgres,database:lvd_stakhanovise_test_db; if_exists=drop)
OUTPUT=markdown_docs(header=parts/readme_db_header.md; footer=parts/readme_db_footer.md; proj=LVD.Stakhanovise.NET; dir=Db/docs; file=README-DB.md; item_group=SK_DbDocs; build_action=None)
OUTPUT=mapping_code(proj=LVD.Stakhanovise.NET.Common.Interfaces; ns=LVD.Stakhanovise.NET.Model; dir=Model; cls=QueuedTaskMapping)