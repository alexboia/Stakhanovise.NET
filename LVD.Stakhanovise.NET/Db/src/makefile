MAP=sk_mapping.dbmap
DEFINITIONS=*.dbdef
OUTPUT=sql_script(dir=/Db/scripts ;mode=consolidated; file=sk_db.sql; build_action=None)
OUTPUT=sql_script(dir=/Setup/BuiltInDbAssetsSetup/scripts; mode=single; file=$db_object$.sql; build_action=Content)
OUTPUT=markdown_docs(dir=/Db/docs; file=README-DB.md)
OUTPUT=create_db(connection_string=host:localhost,port:5432,user:postgres,password:postgres,db:lvd_stakhanovise_test_db; if_exists=drop)
OUTPUT=mapping_code(proj=LVD.Stakhanovise.NET.Common; ns=LVD.Stakhanovise.NET.Model; dir=/Model; cls=QueuedTaskMapping)
OUTPUT=console(func=true; seq=true; tbl=true; func_body=true; tbl_index=true; tbl_unq=true)