set @identity='12345678';
set @secret='abcdef';
set @username='martin@gmail.com';

call sp_AddIdentity(@identity);
call sp_AddCredential(@identity, @username, null, 1, 'HASH', 'SALT');
call sp_AddSharedSecret(@identity, 'API Key', @secret);

call sp_GetIdentity(@identity);
call sp_GetIdentitySharedSecrets(@identity);
call sp_GetIdentitySharedSecrets('invalid');
call sp_GetUserNameCredential(@username);
call sp_GetUserNameCredential('invalid');
call sp_GetSharedSecret(@secret);
call sp_GetSharedSecret('invalid');

call sp_DeleteIdentityCredentials(@identity);
call sp_DeleteSharedSecret(@secret);
