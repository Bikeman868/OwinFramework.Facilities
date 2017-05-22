USE token;

CALL sp_AddToken('abcdefg', 'password-reset', '{"uses":0}');

CALL sp_GetToken('abcdefg');
CALL sp_GetTokenById(1);

CALL sp_UpdateTokenState(1, '{"uses":1}');

SELECT * FROM tbl_token;

CALL sp_DeleteToken(1, null);
CALL sp_DeleteToken(null, 'abcdefg');
CALL sp_DeleteToken(null, null);

CALL sp_clean();
